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
using StardewValley.Pathfinding;
using StardewValley.GameData.Characters;


namespace LittleNPCs.Framework {
    public class LittleNPC : NPC {
        private IMonitor monitor_;

        private static Random random_ = new Random(Game1.Date.TotalDays + (int) Game1.uniqueIDForThisGame / 2 + (int) Game1.MasterPlayer.UniqueMultiplayerID * 2);

        // Check that NPCParseMasterSchedulePatch executed.
        internal bool ParseMasterSchedulePatchExecuted { get; set; }

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

        protected LittleNPC(IMonitor monitor, Child child, AnimatedSprite sprite, Vector2 position, string defaultMap, int facingDir, string name, string displayName, Texture2D portrait, bool eventActor)
        : base(sprite, position, defaultMap, facingDir, name, portrait, eventActor) {
            monitor_ = monitor;
            WrappedChild = child;
            ChildIndex = child.GetChildIndex();

            // Set birthday.
            var birthday = GetBirthday();
            Birthday_Day = birthday.Day;
            Birthday_Season = Utility.getSeasonKey(birthday.Season);

            // Set gender.
            Gender = child.Gender;

            // Set displayName.
            this.displayName = displayName;
        }

        public static LittleNPC FromChild(Child child, FarmHouse farmHouse, IMonitor monitor) {
            Vector2 bedSpot = Utility.PointToVector2(farmHouse.GetChildBedSpot(child.GetChildIndex())) * 64f;
            // (0, 0) means there's noe bed available and the child will stuck in the wall. We must avoid that.
            if (bedSpot == Vector2.Zero) {
                bedSpot = Utility.PointToVector2(farmHouse.getRandomOpenPointInHouse(random_, 1)) * 64f;
                monitor.Log($"No bed spot for {child.Name} found, setting it to random point {Utility.Vector2ToPoint(bedSpot / 64f)}", LogLevel.Warn);
            }

            string prefix = child.GetChildIndex() == 0 ? "FirstLittleNPC" : "SecondLittleNPC";

            var npcDispositions = Game1.content.Load<Dictionary<string, CharacterData>>("Data/Characters");

            var sprite = new AnimatedSprite($"Characters/{prefix}{child.Name}", 0, 16, 32);
            var portrait = Game1.content.Load<Texture2D>($"Portraits/{prefix}{child.Name}");
            var npc = new LittleNPC(monitor,
                                    child,
                                    sprite,
                                    bedSpot,
                                    child.DefaultMap,
                                    child.FacingDirection,
                                    $"{prefix}{child.Name}",
                                    child.Name,
                                    portrait,
                                    false);

            monitor.Log(string.Join(' ',
                                    $"Created LittleNPC {npc.Name}:",
                                    $"index {npc.ChildIndex},",
                                    $"bed spot {Utility.Vector2ToPoint(bedSpot / 64f)},",
                                    $"birthday {new SDate(npc.Birthday_Day, npc.Birthday_Season)}",
                                    $"({npc.WrappedChild.daysOld.Value} days ago)"), LogLevel.Info);

            // Generate and set NPCDispositions.
            // ATTENTION: Don't use CP to set Data/NPCDispositions, you will get into big trouble then.
            // If we add something to 'Data/NPCDispositions' the game attempts to create that NPC.
            // We must control NPC creation, however, so we generate and set dispositions here.
            // Fortunately all important data is provided by the save file.
            // Note that the content pack must not provide NPCDispositions.
            // Example: 
            // child/neutral/outgoing/neutral/male/non-datable/null/Town/summer 23//Farmhouse 23 5/Eric
            // child/neutral/outgoing/neutral/female/non-datable/null/Town/summer 24//Farmhouse 27 5/Sandra
            var characterData = new CharacterData();
            characterData.Age = NpcAge.Child;
            characterData.Manner = NpcManner.Neutral;
            characterData.SocialAnxiety = NpcSocialAnxiety.Outgoing;
            characterData.Optimism = NpcOptimism.Neutral;
            characterData.Gender = npc.Gender == 0 ? NpcGender.Male : NpcGender.Female;
            characterData.CanBeRomanced = false;
            characterData.HomeRegion = "Town";
            characterData.BirthSeason = Enum.Parse<Season>(npc.Birthday_Season, true);
            characterData.BirthDay = npc.Birthday_Day;
            var homeData = new CharacterHomeData();
            homeData.Id = "Default";
            homeData.Location = npc.DefaultMap;
            homeData.Tile = Utility.Vector2ToPoint(bedSpot / 64f);
            characterData.Home = Enumerable.Repeat(homeData, 1).ToList();
            characterData.DisplayName = npc.displayName;

            npcDispositions[npc.Name] = characterData;

            // Subset of character data for logging purposes. Although there's no dispositions string in SDV 1.6 anymore
            // we create something similar because a serialied CharacterData object seems too heavy for just logging.
            string loggedCharacterData
                = string.Join("/",
                              characterData.Age,
                              characterData.Gender,
                              characterData.HomeRegion,
                              $"{characterData.BirthSeason} {characterData.BirthDay}",
                              $"{homeData.Location} {homeData.Tile}",
                              characterData.DisplayName);

            monitor.Log($"Created character data for {npc.Name}: {loggedCharacterData}", LogLevel.Info);

            // ATTENTION: NPC.reloadData() parses dispositions and resets DefaultMap and DefaultPosition for non-married NPCs.
            // This is not a problem since we generated dispositions with matching default values beforehand.
            // We must not call this method in the constructor since it is virtual.
            npc.reloadData();

            // Reload schedule.
            string success = npc.TryLoadSchedule() ? "successfully" : "unsuccessfully";
            monitor.Log($"Schedule or {npc.Name} loaded {success}.", LogLevel.Info);

            // Check if NPCParseMasterSchedulePatch ran.
            if (npc.ParseMasterSchedulePatchExecuted) {
                monitor.Log($"NPCParseMasterSchedulePatch executed for {npc.Name}.", LogLevel.Info);
            }
            else {
                monitor.Log($"NPCParseMasterSchedulePatch didn't execute for {npc.Name}. Schedule won't work.", LogLevel.Warn);

                // NPC's default location might have been messed up on error.
                npc.reloadDefaultLocation();

                monitor.Log($"Reset default location of {npc.Name} to {npc.DefaultMap}, {Utility.Vector2ToPoint(npc.DefaultPosition / 64f)}.", LogLevel.Warn);
            }

            return npc;
        }

        /// <inheritdoc/>
        public override void performTenMinuteUpdate(int timeOfDay, GameLocation l) {
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

        public override void arriveAtFarmHouse (FarmHouse farmHouse) {
            if (ModEntry.config_.DoChildrenHaveCurfew && Game1.timeOfDay >= ModEntry.config_.CurfewTime) {
                Point bedPoint = Utility.Vector2ToPoint(DefaultPosition / 64f);

                // If farmer is not here move to bed instantly because path finding will be cancelled when the farmer enters the house.
                // Note that we need PathFindController even in this case because setTilePosition(bedPoint) places the NPC next to bed.
                if (Game1.player.currentLocation != farmHouse) {
                    setTilePosition(bedPoint);
                }
                else {
                    setTilePosition(farmHouse.getEntryLocation());
                }

                ignoreScheduleToday = true;
                temporaryController = null;
                // In order to make path finding work we must assign null first.
                controller = null;

                controller = new PathFindController(this, farmHouse, bedPoint, 2);
            }
            else {
                setTilePosition(farmHouse.getEntryLocation());

                ignoreScheduleToday = true;
                temporaryController = null;
                controller = null;

                controller = new PathFindController(this, farmHouse, farmHouse.getRandomOpenPointInHouse(Game1.random, 0, 30), 2);
            }

            if (controller.pathToEndPoint is null) {
                willDestroyObjectsUnderfoot = true;
                controller = new PathFindController(this, farmHouse, farmHouse.getRandomOpenPointInHouse(Game1.random, 0, 30), 0);
            }

            if (Game1.currentLocation == farmHouse) {
                Game1.currentLocation.playSound("doorClose", null, null, StardewValley.Audio.SoundContext.NPC);
            }
        }

        public override void checkSchedule(int timeOfDay) {
            if (currentScheduleDelay == 0f && scheduleDelaySeconds > 0f) {
                currentScheduleDelay = scheduleDelaySeconds;
            }
            else {
                if (returningToEndPoint) {
                    return;
                }

                updatedDialogueYet = false;
                extraDialogueMessageToAddThisMorning = null;
                if (ignoreScheduleToday || Schedule is null) {
                    return;
                }

                SchedulePathDescription value = null;
                if (lastAttemptedSchedule < timeOfDay) {
                    lastAttemptedSchedule = timeOfDay;
                    Schedule.TryGetValue(timeOfDay, out value);
                    if (value is not null) {
                        queuedSchedulePaths.Add(value);
                    }
                    value = null;
                }

                // If I have curfew, override the normal behavior.
                if (ModEntry.config_.DoChildrenHaveCurfew && !currentLocation.Equals(Game1.getLocationFromName("FarmHouse"))) {
                    // Send child home for curfew.
                    if(timeOfDay == ModEntry.config_.CurfewTime) {
                        value = pathfindToNextScheduleLocation(null, currentLocation.Name, (int) Tile.X, (int) Tile.Y, "BusStop", -1, 23, 3, null, null);
                        queuedSchedulePaths.Clear();
                        queuedSchedulePaths.Add(value);
                    }
                    value = null;
                }

                if (controller is not null && controller.pathToEndPoint is not null && controller.pathToEndPoint.Count > 0) {
                    return;
                }

                if (queuedSchedulePaths.Count > 0 && timeOfDay >= queuedSchedulePaths[0].time) {
                    value = queuedSchedulePaths[0];
                }

                if (value is null) {
                    return;
                }

                prepareToDisembarkOnNewSchedulePath();
                if (returningToEndPoint || temporaryController is not null) {
                    return;
                }

                DirectionsToNewLocation = value;
                if (queuedSchedulePaths.Count > 0) {
                    queuedSchedulePaths.RemoveAt(0);
                }

                controller = new PathFindController(DirectionsToNewLocation.route, this, Utility.getGameLocationOfCharacter(this)) {
                    finalFacingDirection = DirectionsToNewLocation.facingDirection,
                    endBehaviorFunction = getRouteEndBehaviorFunction(DirectionsToNewLocation.endOfRouteBehavior, DirectionsToNewLocation.endOfRouteMessage)
                };

                if (controller.pathToEndPoint is null || controller.pathToEndPoint.Count == 0) {
                    if (controller.endBehaviorFunction is not null) {
                        controller.endBehaviorFunction(this, currentLocation);
                    }
                    controller = null;
                }

                if (DirectionsToNewLocation is not null && DirectionsToNewLocation.route is not null) {
                    previousEndPoint = (DirectionsToNewLocation.route.Count > 0) ? DirectionsToNewLocation.route.Last() : Point.Zero;
                }
            }
        }

        public override Dictionary<int, SchedulePathDescription> parseMasterSchedule(string scheduleKey, string rawData) {
            // Scheduling code can use "bed" to refer to the usual last stop of an NPC.
            // For a LittleNPC, this is always the bus stop, so I can just do the replacement here.
            if (rawData.EndsWith("bed")) {
                rawData = rawData[..^3] + "BusStop -1 23 3";
            }

            // Save the previous default map and default position.
            string previousDefaultMap = DefaultMap;
            var previousDefaultPosition = DefaultPosition;

            // Pretending my start location is the bus stop location.
            DefaultMap = "BusStop";
            DefaultPosition = new Vector2(0, 23) * 64;

            Dictionary<int, SchedulePathDescription> retval = null;
            try {
                retval = base.parseMasterSchedule(scheduleKey, rawData);

                ParseMasterSchedulePatchExecuted = true;
            }
            finally {
                DefaultMap = previousDefaultMap;
                DefaultPosition = previousDefaultPosition;
            }

            return retval;
        }

        protected override void prepareToDisembarkOnNewSchedulePath() {
            if (Utility.getGameLocationOfCharacter(this) is FarmHouse) {
                temporaryController = new PathFindController(this, getHome(), new Point(getHome().warps[0].X, getHome().warps[0].Y), 2, true) {
                    NPCSchedule = true
                };
                if (temporaryController.pathToEndPoint is null || temporaryController.pathToEndPoint.Count <= 0) {
                    temporaryController = null;
                    ClearSchedule();
                }
                else {
                    followSchedule = true;
                }
            }
            else if (Utility.getGameLocationOfCharacter(this) is Farm) {
                temporaryController = null;
                ClearSchedule();
            }
        }

        public SDate GetBirthday() {
            return GetBirthday(WrappedChild);
        }

        public static SDate GetBirthday(Child child) {
            SDate birthday;

            try {
                // Subtract age of child in days from current date.
                birthday = SDate.Now().AddDays(-child.daysOld.Value);
            }
            catch (ArithmeticException) {
                // Fallback.
                birthday = new SDate(1, "spring");
            }

            return birthday;
        }
    }
}
