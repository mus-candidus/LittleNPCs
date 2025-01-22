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
    public class LittleNPC : NPC {
        /// <summary>
        /// This class is used to transfer an image as JSON.
        /// </summary>
        /// <param name="Width">Texture width</param>
        /// <param name="Height">Texture height</param>
        /// <param name="Data">Texture data. Using <code>uint</code> instead of <code>Color</code> to get more compact JSON.</param>
        public record class TransferImage(int Width, int Height, uint[] Data) {
            /// <summary>
            /// Factory method instead of constructor.
            /// This class already has a primary constructor and defining another one would prevent JSON serialization.
            /// </summary>
            /// <param name="texture"></param>
            /// <returns></returns>
            public static TransferImage FromTexture(Texture2D texture) {
                var result = new TransferImage(texture.Width, texture.Height,new uint[texture.Width * texture.Height]);
                texture.GetData(result.Data);

                return result;
            }

            public Texture2D ToTexture(GraphicsDevice graphicsDevice) {
                var texture = new Texture2D(graphicsDevice, Width, Height);
                texture.SetData(Data);

                return texture;
            }
        }

        private record class TransferData(CharacterData CharacterData,
                                          Dictionary<string, string> MasterScheduleData,
                                          TransferImage Sprite,
                                          TransferImage Portrait,
                                          Dictionary<string, string> Dialogue) {
        }

        private static Random random_ = new Random(Game1.Date.TotalDays + (int) Game1.uniqueIDForThisGame / 2 + (int) Game1.MasterPlayer.UniqueMultiplayerID * 2);

        // Check that NPCParseMasterSchedulePatch executed.
        private bool ParseMasterSchedulePatchExecuted { get; set; }

        /// <summary>
        /// Wrapped child object. Required to replace the corresponding LittleNPC object on save.
        /// </summary>
        /// <value></value>
        private readonly NetRef<Hat> wrappedChildHat_ = new NetRef<Hat>();

        private readonly NetString transferDataJson_ = new NetString();

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
                          .AddField(transferDataJson_);

            transferDataJson_.fieldChangeVisibleEvent += (self, oldValue, newValue) => {
                if (newValue is not null) {
                    AssignTransferData(newValue);
                }
            };
        }

        /// <summary>
        /// Cached child index. The method <code>Child.GetChildIndex()</code>
        /// becomes useless after removing any child object so we must cache this value.
        /// </summary>
        /// <value></value>
        public int ChildIndex { get; private set; }

        /// <summary>
        /// Determines whether it's time for a child to go to bed.
        /// </summary>
        private bool IsTimeForBed {
            get => (ModEntry.config_.DoChildrenHaveCurfew && Game1.timeOfDay >= ModEntry.config_.CurfewTime) || (!ModEntry.config_.DoChildrenHaveCurfew && Game1.timeOfDay >= 2130);
        }

        public LittleNPC() {
        }

        protected LittleNPC(Child child,
                            AnimatedSprite sprite,
                            Vector2 position,
                            string defaultMap,
                            int facingDir,
                            string name,
                            Texture2D portrait)
        : base(sprite, position, defaultMap, facingDir, name, portrait, false) {
            // Take hat off because it stays visible even when making a child invisible.
            if (child.hat.Value is not null) {
                wrappedChildHat_.Value = child.hat.Value;
                child.hat.Value = null;
            }

            ChildIndex = child.GetChildIndex();

            // Set birthday.
            var birthday = GetBirthday(child);
            Birthday_Day = birthday.Day;
            Birthday_Season = Utility.getSeasonKey(birthday.Season);

            // Ensure that the original child stays invisible.
            if (!child.IsInvisible) {
                ModEntry.monitor_.Log($"[{GetHostTag()}] Made child {child.Name} invisible.", LogLevel.Info);
                child.IsInvisible = true;
            }
        }

        public static LittleNPC FromChild(Child child, FarmHouse farmHouse, IMonitor monitor) {
            Vector2 bedSpot = Utility.PointToVector2(farmHouse.GetChildBedSpot(child.GetChildIndex())) * 64f;
            // (0, 0) means there's noe bed available and the child will stuck in the wall. We must avoid that.
            if (bedSpot == Vector2.Zero) {
                bedSpot = Utility.PointToVector2(farmHouse.getRandomOpenPointInHouse(random_, 1)) * 64f;
                monitor.Log($"[{GetHostTag()}] No bed spot for {child.Name} found, setting it to random point {Utility.Vector2ToPoint(bedSpot / 64f)}", LogLevel.Warn);
            }

            string assetName = LittleNPCInfo.CreateInternalAssetName(child.GetChildIndex(), child.Name);

            AnimatedSprite sprite = new AnimatedSprite($"Characters/{assetName}", 0, 16, 32);
            Texture2D portrait = Game1.content.Load<Texture2D>($"Portraits/{assetName}");

            // ATTENTION: DefaultMap of child is just FarmHouse which is wrong for farmhands.
            var npc = new LittleNPC(child,
                                    sprite,
                                    bedSpot,
                                    farmHouse.NameOrUniqueName,
                                    child.FacingDirection,
                                    assetName,
                                    portrait);

            // Set gender. Virtual property, can't be set in the constructor.
            npc.Gender = child.Gender;

            // Set displayName. Virtual property, can't be set in the constructor.
            npc.displayName = child.Name;

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
            characterData.Breather = false;

            // Load schedule to put it into a NetRef.
            npc.getMasterScheduleRawData();

            // Serialize it to JSON to transmit it over the wire.
            TransferData transferData = new TransferData(characterData,
                                                         npc._masterScheduleData,
                                                         TransferImage.FromTexture(sprite.spriteTexture),
                                                         TransferImage.FromTexture(portrait),
                                                         npc.Dialogue);

            string transferDataJson = JsonConvert.SerializeObject(transferData);

            // Set NetRef triggers assignment event.
            npc.transferDataJson_.Value = transferDataJson;

            return npc;
        }

        private void AssignTransferData(string transferDataJson) {
            TransferData transferData = JsonConvert.DeserializeObject<TransferData>(transferDataJson);

            CharacterData characterData = transferData.CharacterData;
            var npcDispositions = Game1.characterData;

            if (!npcDispositions.TryGetValue(Name, out _)) {
                // Assign new character data to trigger NPC creation. Breather is a bit special: It's part of character data
                // AND it must be set for the NPC. SDV does that internally when a corresponding Data/Characters entry exists.
                // That's not the case here so we must do it explicitly.
                npcDispositions[Name] = characterData;
                Breather = characterData.Breather;

                var loggedCharacterData = CharacterDataToString(characterData);

                ModEntry.monitor_.Log($"[{GetHostTag()}] Created character data for {Name}: {loggedCharacterData}", LogLevel.Info);
            }
            else {
                // Data was loaded from a corresponding Data/Characters section. Assign only the fields that must be controlled by the mod,
                // everything else can be configured by the content pack.
                npcDispositions[Name].Age = characterData.Age;
                npcDispositions[Name].Gender = characterData.Gender;
                npcDispositions[Name].CanBeRomanced = characterData.CanBeRomanced;
                npcDispositions[Name].HomeRegion = characterData.HomeRegion;
                npcDispositions[Name].BirthSeason = characterData.BirthSeason;
                npcDispositions[Name].BirthDay = characterData.BirthDay;
                npcDispositions[Name].CanReceiveGifts = characterData.CanReceiveGifts;
                npcDispositions[Name].Home = characterData.Home;
                npcDispositions[Name].DisplayName = characterData.DisplayName;

                var loggedCharacterData = CharacterDataToString(characterData);

                ModEntry.monitor_.Log($"[{GetHostTag()}] Found and modified existing character data for {Name}: {loggedCharacterData}", LogLevel.Info);
            }

            ModEntry.CachedAssets[$"Characters/{Name}"] = transferData.Sprite;
            ModEntry.CachedAssets[$"Portraits/{Name}"] = transferData.Portrait;
            ModEntry.CachedAssets[$"Characters/Dialogue/{Name}"] = transferData.Dialogue;

            // Setting schedules is not necessary on multiplayer clients, all schedules run on the host.
            if (!Game1.IsMasterGame) {
                return;
            }

            // Make getMasterScheduleRawData() work without overriding it (which wouldn't be possible anyway).
            _masterScheduleData = transferData.MasterScheduleData;
            _hasLoadedMasterScheduleData = true;

            // Reload schedule.
            string success = TryLoadSchedule() ? "successfully" : "unsuccessfully";
            ModEntry.monitor_.Log($"[{GetHostTag()}] Schedule for {Name} loaded {success}.", LogLevel.Info);

            // Check if NPCParseMasterSchedulePatch ran.
            if (ParseMasterSchedulePatchExecuted) {
                ModEntry.monitor_.Log($"[{GetHostTag()}] NPCParseMasterSchedulePatch executed for {Name}.", LogLevel.Info);
            }
            else {
                ModEntry.monitor_.Log($"[{GetHostTag()}] NPCParseMasterSchedulePatch didn't execute for {Name}. Schedule won't work.", LogLevel.Warn);

                // NPC's default location might have been messed up on error.
                reloadDefaultLocation();

                ModEntry.monitor_.Log($"[{GetHostTag()}] Reset default location of {Name} to {DefaultMap}, {Utility.Vector2ToPoint(DefaultPosition / 64f)}.", LogLevel.Warn);
            }
        }

        /// <summary>
        /// Subset of character data for logging purposes. Although there's no dispositions string in SDV 1.6 anymore
        /// we create something similar because a serialized CharacterData object seems too heavy for just logging.
        /// </summary>
        /// <param name="characterData"></param>
        /// <returns></returns>
        private static string CharacterDataToString(CharacterData characterData) {
            string loggedCharacterData
                = string.Join("/",
                    characterData.Age,
                    characterData.Gender,
                    characterData.HomeRegion,
                    $"{characterData.BirthSeason} {characterData.BirthDay}",
                    $"{characterData.Home.First().Location} {characterData.Home.First().Tile}",
                    characterData.DisplayName);
            return loggedCharacterData;
        }

        /// <inheritdoc/>
        public override void performTenMinuteUpdate(int timeOfDay, GameLocation l) {
            FarmHouse farmHouse = getHome() as FarmHouse;
            if (farmHouse?.characters.Contains(this) ?? false) {
                Point bedPoint = new Point((int) DefaultPosition.X / 64, (int) DefaultPosition.Y / 64);

                // Send children to bed when inside home.
                if (IsTimeForBed && TilePoint != bedPoint) {
                    IsWalkingInSquare = false;
                    Halt();
                    temporaryController = null;

                    // Child is at home, direct path to bed (DefaultPosition).
                    // The original Child object might be hidden but reserves the bed nonetheless so we don't need to call ReserveForNPC() .
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
                         && ModEntry.config_.DoChildrenWander
                         && (Schedule is null || !Schedule.ContainsKey(Game1.timeOfDay))
                         && Game1.timeOfDay % 100 == 0
                         && !IsTimeForBed) {
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

        /// <summary>
        /// Prevent children from walking into the void.
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public override bool shouldCollideWithBuildingLayer(GameLocation location) {
            if (Schedule == null || location is FarmHouse) {
                return true;
            }

            return base.shouldCollideWithBuildingLayer(location);
        }

        public override void arriveAtFarmHouse(FarmHouse farmHouse) {
            Point bedPoint = Utility.Vector2ToPoint(DefaultPosition / 64f);
            if (Game1.newDay || Game1.timeOfDay <= 630 || TilePoint == bedPoint) {
                return;
            }

            setTilePosition(farmHouse.getEntryLocation());
            ignoreScheduleToday = true;
            temporaryController = null;
            // In order to make path finding work we must assign null first.
            controller = null;

            if (IsTimeForBed) {
                // If farmer is not here move to bed instantly because path finding will be cancelled when the farmer enters the house.
                // Note that we need PathFindController even in this case because setTilePosition(bedPoint) places the NPC next to bed.
                if (!Game1.player.currentLocation.Equals(farmHouse)) {
                    setTilePosition(bedPoint);
                }
                else {
                    setTilePosition(farmHouse.getEntryLocation());
                }

                // Create a PathFindController to bed spot.
                controller = new PathFindController(this, farmHouse, bedPoint, 2);
            }
            else {
                controller = new PathFindController(this, farmHouse, farmHouse.getRandomOpenPointInHouse(Game1.random, 0, 30), 2);
            }
            if (controller.pathToEndPoint is null) {
                willDestroyObjectsUnderfoot = true;
                controller = new PathFindController(this, farmHouse, farmHouse.getRandomOpenPointInHouse(Game1.random, 0, 30), 2);
            }

            if (Game1.currentLocation.Equals(farmHouse)) {
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
                if (ModEntry.config_.DoChildrenHaveCurfew && !currentLocation.Equals(getHome())) {
                    // Send child home for curfew.
                    if(timeOfDay == ModEntry.config_.CurfewTime) {
                        value = pathfindToNextScheduleLocation(null, currentLocation.Name, (int) Tile.X, (int) Tile.Y, "BusStop", 9, 23, 3, null, null);
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
                rawData = rawData[..^3] + "BusStop 9 23 3";
            }

            // Save the previous default map and default position.
            string previousDefaultMap = DefaultMap;
            var previousDefaultPosition = DefaultPosition;

            // Pretending my start location is the bus stop location.
            DefaultMap = "BusStop";
            DefaultPosition = new Vector2(10, 23) * 64;

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
            // Base method only works for married NPCs but we must call it to stop running animations.
            base.prepareToDisembarkOnNewSchedulePath();
            // This is normally only for married NPCs.
            if (Utility.getGameLocationOfCharacter(this) is FarmHouse) {
                var home = getHome();
                ModEntry.monitor_.VerboseLog($"[{GetHostTag()}] prepareToDisembarkOnNewSchedulePath: {home.NameOrUniqueName}");
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
            ModEntry.monitor_.Log($"[{GetHostTag()}] MasterScheduleFileLoadError: {e.Message}", LogLevel.Error);
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

        internal static string GetHostTag()
        {
            return Game1.IsMasterGame ? "Host" : "Client";
        }
    }
}
