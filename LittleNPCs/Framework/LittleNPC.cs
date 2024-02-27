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
using StardewValley.Objects;
using StardewValley.Pathfinding;
using StardewValley.GameData.Characters;

using Netcode;
using Newtonsoft.Json;


namespace LittleNPCs.Framework {
    record class TransferData(CharacterData CharacterData, Dictionary<string, string> MasterScheduleData) {
    }

    public class LittleNPC : NPC {
        private static Random random_ = new Random(Game1.Date.TotalDays + (int) Game1.uniqueIDForThisGame / 2 + (int) Game1.MasterPlayer.UniqueMultiplayerID * 2);

        // Check that NPCParseMasterSchedulePatch executed.
        internal bool ParseMasterSchedulePatchExecuted { get; set; }

        /// <summary>
        /// Wrapped child object. Required to replace the corresponding LittleNPC object on save.
        /// </summary>
        /// <value></value>
        private readonly NetRef<Hat> wrappedChildHat_ = new NetRef<Hat>();

        private readonly NetLong idOfParent_ = new NetLong();

        private readonly NetString transferDataJson_ = new NetString();

        internal long IdOfParent {
            get => idOfParent_.Value;
        }
        
        /// <summary>
        /// Wrapped child's hat, if any. Must be removed during the day.
        /// </summary>
        /// <value></value>
        public Hat WrappedChildHat {
            get => wrappedChildHat_.Value;
        }


        protected override void initNetFields() {
            base.initNetFields();
            base.NetFields.AddField(wrappedChildHat_)
                          .AddField(idOfParent_)
                          .AddField(transferDataJson_);

            transferDataJson_.fieldChangeVisibleEvent += (self, oldValue, newValue) => {
                if (newValue is not null) {
                    AssignTransferData(newValue);
                    ModEntry.monitor_.Log($"AssignCharacterData() finished.", LogLevel.Warn);
                }
            };
        }

        /// <summary>
        /// Cached child index. The method <code>Child.GetChildIndex()</code>
        /// becomes useless after removing any child object so we must cache this value.
        /// </summary>
        /// <value></value>
        public int ChildIndex { get; private set; }

        public LittleNPC() {
        }

        protected LittleNPC(Child child,
                            AnimatedSprite sprite,
                            Vector2 position,
                            string defaultMap,
                            int facingDir,
                            string name,
                            string displayName,
                            Texture2D portrait)
        : base(sprite, position, defaultMap, facingDir, name, portrait, false) {
            // Take hat off because it stays visible even when making a child invisible.
            if (child.hat.Value is not null) {
                wrappedChildHat_.Value = child.hat.Value;
                child.hat.Value = null;
            }

            ChildIndex = child.GetChildIndex();

            idOfParent_.Value = child.idOfParent.Value;

            // Set birthday.
            var birthday = GetBirthday(child);
            Birthday_Day = birthday.Day;
            Birthday_Season = Utility.getSeasonKey(birthday.Season);

            // Set gender.
            Gender = child.Gender;

            // Set displayName.
            this.displayName = displayName;

            // Ensure that the original child stays invisible.
            if (!child.IsInvisible) {
                ModEntry.monitor_.Log($"Made child {child.Name} invisible.", LogLevel.Info);
                child.IsInvisible = true;
            }
        }

        public static LittleNPC FromChild(Child child, FarmHouse farmHouse, IMonitor monitor) {
            Vector2 bedSpot = Utility.PointToVector2(farmHouse.GetChildBedSpot(child.GetChildIndex())) * 64f;
            // (0, 0) means there's noe bed available and the child will stuck in the wall. We must avoid that.
            if (bedSpot == Vector2.Zero) {
                bedSpot = Utility.PointToVector2(farmHouse.getRandomOpenPointInHouse(random_, 1)) * 64f;
                monitor.Log($"No bed spot for {child.Name} found, setting it to random point {Utility.Vector2ToPoint(bedSpot / 64f)}", LogLevel.Warn);
            }

            string assetName = LittleNPCInfo.CreateInternalAssetName(child.GetChildIndex(), child.Name);

            AnimatedSprite sprite = new AnimatedSprite($"Characters/{assetName}", 0, 16, 32);
            Texture2D portrait = Game1.content.Load<Texture2D>($"Portraits/{assetName}");

            var npc = new LittleNPC(child,
                                    sprite,
                                    bedSpot,
                                    child.DefaultMap,
                                    child.FacingDirection,
                                    assetName,
                                    child.Name,
                                    portrait);

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
            characterData.Gender = npc.Gender;
            characterData.CanBeRomanced = false;
            characterData.HomeRegion = "Town";
            characterData.BirthSeason = Enum.Parse<Season>(npc.Birthday_Season, true);
            characterData.BirthDay = npc.Birthday_Day;
            characterData.CanReceiveGifts = true;
            var homeData = new CharacterHomeData();
            homeData.Id = "Default";
            homeData.Location = farmHouse.NameOrUniqueName;
            homeData.Tile = Utility.Vector2ToPoint(bedSpot / 64f);
            characterData.Home = Enumerable.Repeat(homeData, 1).ToList();
            characterData.DisplayName = npc.displayName;

            // Load schedule to put it into a NetRef.
            string success = npc.TryLoadSchedule() ? "successfully" : "unsuccessfully";
            ModEntry.monitor_.Log($"Schedule for {assetName} loaded {success}.", LogLevel.Info);

            // Serialize it to JSON to transmit it over the wire.
            TransferData transferData = new TransferData(characterData, npc._masterScheduleData);

            string transferDataJson = JsonConvert.SerializeObject(transferData);

            // Set NetRef triggers assignment event.
            npc.transferDataJson_.Value = transferDataJson;

            return npc;
        }

        private void AssignTransferData(string transferDataJson) {
            ModEntry.monitor_.Log($"AssignCharacterData() started.", LogLevel.Warn);

            TransferData transferData = JsonConvert.DeserializeObject<TransferData>(transferDataJson);

            CharacterData characterData = transferData.CharacterData;
            var npcDispositions = Game1.content.Load<Dictionary<string, CharacterData>>("Data/Characters");

            if (npcDispositions.TryGetValue(Name, out _)) {
                ModEntry.monitor_.Log($"Character data for {Name} already set, skipping.", LogLevel.Warn);

                //return;
            }
            else {
                npcDispositions[Name] = characterData;

                // Subset of character data for logging purposes. Although there's no dispositions string in SDV 1.6 anymore
                // we create something similar because a serialied CharacterData object seems too heavy for just logging.
                string loggedCharacterData
                    = string.Join("/",
                                  characterData.Age,
                                  characterData.Gender,
                                  characterData.HomeRegion,
                                  $"{characterData.BirthSeason} {characterData.BirthDay}",
                                  $"{characterData.Home.First().Location} {characterData.Home.First().Tile}",
                                  characterData.DisplayName);

                ModEntry.monitor_.Log($"Created character data for {Name}: {loggedCharacterData}", LogLevel.Info);
            }

            _masterScheduleData = transferData.MasterScheduleData;

            // ATTENTION: NPC.reloadData() parses dispositions and resets DefaultMap and DefaultPosition for non-married NPCs.
            // This is not a problem since we generated dispositions with matching default values beforehand.
            // We must not call this method in the constructor since it is virtual.
            reloadData();

            // Reload schedule.
            string success = TryLoadSchedule() ? "successfully" : "unsuccessfully";
            ModEntry.monitor_.Log($"Schedule for {Name} loaded {success}.", LogLevel.Info);

            // Check if NPCParseMasterSchedulePatch ran.
            if (ParseMasterSchedulePatchExecuted) {
                ModEntry.monitor_.Log($"NPCParseMasterSchedulePatch executed for {Name}.", LogLevel.Info);
            }
            else {
                ModEntry.monitor_.Log($"NPCParseMasterSchedulePatch didn't execute for {Name}. Schedule won't work.", LogLevel.Warn);

                // NPC's default location might have been messed up on error.
                reloadDefaultLocation();

                ModEntry.monitor_.Log($"Reset default location of {Name} to {DefaultMap}, {Utility.Vector2ToPoint(DefaultPosition / 64f)}.", LogLevel.Warn);
            }
        }

        /// <inheritdoc/>
        public override void performTenMinuteUpdate(int timeOfDay, GameLocation l) {
            //FarmHouse farmHouse = Utility.getHomeOfFarmer(Game1.player);
            FarmHouse farmHouse = Utility.getHomeOfFarmer(Game1.getFarmerMaybeOffline(IdOfParent));
            if (farmHouse?.characters.Contains(this) ?? false) {
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
                //if (ModEntry.config_.DoChildrenHaveCurfew && !currentLocation.Equals(Game1.getLocationFromName(defaultLocationName_.Value, true))) {
                if (ModEntry.config_.DoChildrenHaveCurfew && !currentLocation.Equals(Utility.getHomeOfFarmer(Game1.getFarmerMaybeOffline(IdOfParent)))) {
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
                //var home = getHome();
                var home = Utility.getHomeOfFarmer(Game1.getFarmerMaybeOffline(IdOfParent));
                ModEntry.monitor_.Log($"prepareToDisembarkOnNewSchedulePath: {home.NameOrUniqueName}", LogLevel.Warn);
                temporaryController = new PathFindController(this, home, new Point(home.warps[0].X, home.warps[0].Y), 2, true) {
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

        public override void handleMasterScheduleFileLoadError(Exception e) {
            ModEntry.monitor_.Log($"MasterScheduleFileLoadError: {e}", LogLevel.Error);
        }

        public SDate GetBirthday() {
            return new SDate(Birthday_Day, Birthday_Season);
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
