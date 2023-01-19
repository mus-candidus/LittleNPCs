using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI;
using StardewModdingAPI.Utilities;

using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;


namespace LittleNPCs.Framework {
    public class LittleNPC : NPC {
        private IMonitor monitor_;

        /// <summary>
        /// Wrapped child object. Required to replace the corresponding LittleNPC object on save.
        /// </summary>
        /// <value></value>
        public Child WrappedChild { get; private set; }

        /// <summary>
        /// Cached child index. The method <code>Child.GetChildIndex()</code>
        /// becomes useless after removing any child object so we must cache this value.
        /// </summary>
        /// <value></value>
        public int ChildIndex { get; private set; }

        protected LittleNPC(IMonitor monitor, Child child, int childIndex, AnimatedSprite sprite, Vector2 position, string defaultMap, int facingDir, string name, Dictionary<int, int[]> schedule, Texture2D portrait, bool eventActor)
        : base(sprite, position, defaultMap, facingDir, name, schedule, portrait, eventActor, null) {
            monitor_ = monitor;
            WrappedChild = child;
            ChildIndex = childIndex;

            // Set birthday.
            var birthday = GetBirthday();
            Birthday_Day = birthday.Day;
            Birthday_Season = birthday.Season;

            // Set gender.
            Gender = child.Gender;

            monitor_.Log($"LittleNPC.ctor {Schedule}", LogLevel.Warn);
        }

        public static LittleNPC FromChild(Child child, int childIndex, FarmHouse farmHouse, IMonitor monitor) {
            Vector2 bedSpot = Utility.PointToVector2(farmHouse.GetChildBedSpot(childIndex)) * 64f;

            var npcDispositions = Game1.content.Load<Dictionary<string, string>>("Data/NPCDispositions");

            var sprite = new AnimatedSprite($"Characters/{child.Name}", 0, 16, 32);
            var portrait = Game1.content.Load<Texture2D>($"Portraits/{child.Name}");
            var npc = new LittleNPC(monitor,
                                    child,
                                    childIndex,
                                    sprite,
                                    bedSpot,
                                    child.DefaultMap,
                                    child.FacingDirection,
                                    child.Name,
                                    null,
                                    portrait,
                                    false);

            monitor.Log($"LittleNPC.FromChild {child.Name} {GetBedSpot(farmHouse, childIndex)} {childIndex} {child.daysOld.Value}", LogLevel.Warn);

            // Set dispositions now.
            // ATTENTION: Don't use CP to set Data/NPCDispositions, you will get into big trouble then.
            // Reason: If we add something to 'Data/NPCDispositions' the game attempts to create that NPC.
            // We must control NPC creation, however, so we auto-generate and set dispositions here.
            // Note that the content pack must not provide NPCDispositions.
            // It doesn't make sense, anyway: All important data is provided by the save file.
            // Example: 
            // child/neutral/outgoing/neutral/male/non-datable/null/Town/summer 23//Farmhouse 23 5/Eric
            // child/neutral/outgoing/neutral/female/non-datable/null/Town/summer 24//Farmhouse 27 5/Sandra
            npcDispositions[npc.Name] = $"child/neutral/outgoing/neutral/{(npc.Gender == 0 ? "male": "female")}/non-datable/null/Town/{npc.Birthday_Season} {npc.Birthday_Day}//{npc.DefaultMap} {(int) LittleNPC.GetBedSpot(Utility.getHomeOfFarmer(Game1.player), childIndex).X / 64f} {(int) LittleNPC.GetBedSpot(Utility.getHomeOfFarmer(Game1.player), childIndex).Y / 64f}/{npc.Name}";
            monitor.Log($"{npcDispositions[npc.Name]}", LogLevel.Warn);

            // ATTENTION: NPC.reloadData() parses dispositions and resets DefaultMap and DefaultPosition for non-married NPCs.
            // This is not a problem since we generated dispositions with matching default values beforehand.
            // We must not call this method in the constructor since it is virtual.
            npc.reloadData();

            // Reload schedule.
            npc.Schedule = npc.getSchedule(Game1.dayOfMonth);

            return npc;
        }

        /// <inheritdoc/>
        public override void performTenMinuteUpdate(int timeOfDay, GameLocation l) {
            monitor_.Log($"LittleNPC.performTenMinuteUpdate {this.Name} {this.currentLocation}, {Utility.Vector2ToPoint(this.Position / 64f)}", LogLevel.Warn);

            FarmHouse farmHouse = Utility.getHomeOfFarmer(Game1.player);
            if (farmHouse.characters.Contains(this)) {
                ModConfig config = ModEntry.config_;
                // Send children to bed when inside home.
                if (config.DoChildrenHaveCurfew && Game1.timeOfDay == config.CurfewTime) {
                    IsWalkingInSquare = false;
                    Halt();
                    temporaryController = null;

                    // Child is at home, direct path to bed (DefaultPosition).
                    Point bedPoint = new Point((int) DefaultPosition.X / 64, (int) DefaultPosition.Y / 64);
                    controller = new PathFindController(this, farmHouse, bedPoint, 2);

                    if (controller.pathToEndPoint is null || !farmHouse.isTileOnMap(controller.pathToEndPoint.Last().X, controller.pathToEndPoint.Last().Y)) {
                        controller = null;
                    }
                }
                // Make children wander if they have nothing better to do.
                // ATTENTION: We have to skip that for scheduled times, otherwise schedule and random wandering overlap in a weird way:
                // The NPCs get warped out of farm house before they reach their random destination points in the house
                // and thus are doomed to walk around in the BusStop location endlessly without a chance to reach their destination!
                else if (controller is null
                         && config.DoChildrenWander
                         && (Schedule is null || !Schedule.ContainsKey(Game1.timeOfDay))
                         && Game1.timeOfDay % 100 == 0
                         && Game1.timeOfDay < config.CurfewTime) {
                    if (!currentLocation.Equals(Utility.getHomeOfFarmer(Game1.player))) {
                        return;
                    }

                    IsWalkingInSquare = false;
                    Halt();

                    // If I'm going to prevent them from wandering into doorways, I need to do it here.
                    controller = new PathFindController(this, farmHouse, farmHouse.getRandomOpenPointInHouse(Game1.random, 0, 30), 2);
                    if (controller.pathToEndPoint is null || !farmHouse.isTileOnMap(controller.pathToEndPoint.Last().X, controller.pathToEndPoint.Last().Y)) {
                        controller = null;
                    }
                }
            }

            // Call base method.
            base.performTenMinuteUpdate(timeOfDay, l);
        }

        public SDate GetBirthday() {
            SDate birthday;

            try {
                // Subtract age of child in days from current date.
                birthday = SDate.Now().AddDays(-WrappedChild.daysOld.Value);
            }
            catch (ArithmeticException) {
                // Fallback.
                birthday = new SDate(1, "spring");
            }

            return birthday;
        }

        public static SDate BirthdayFromDays(int daysOld) {
            try {
                // Subtract age of child in days from current date.
                return SDate.Now().AddDays(-daysOld);
            }
            catch (ArithmeticException) {
                // Fallback.
                return new SDate(1, "spring");
            }
        }

        /// <summary>
        /// Helper function used by constructor.
        /// </summary>
        /// <param name="farmHouse"></param>
        /// <param name="childIndex"></param>
        /// <returns></returns>
        public static Vector2 GetBedSpot(FarmHouse farmHouse, int childIndex) {
            return Utility.PointToVector2(farmHouse.GetChildBedSpot(childIndex)) * 64f;
        }
    }
}
