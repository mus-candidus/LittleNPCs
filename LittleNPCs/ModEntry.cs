using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;

using LittleNPCs.Framework;


namespace LittleNPCs {
    public class ModEntry : Mod {
        public static IMonitor monitor_;

        public static ModConfig config_;

        private int? relativeSeconds_;

        // We have to keep track of LittleNPCs vor various reasons.
        public static Dictionary<LittleNPC, Child> TrackedLittleNPCs { get; } = new Dictionary<LittleNPC, Child>();

        public static Dictionary<string, object> CachedAssets { get; } = new Dictionary<string, object>();

        public override void Entry(IModHelper helper) {
            ModEntry.monitor_ = this.Monitor;

            // Check for LittleNPC content packs. This is quite heavy but the only way I know so far:
            // We have to check for ContentPatcher packs that depend on LittleNPCs.
            var contentPatcherPacks = from entry in helper.ModRegistry.GetAll()
                                      where entry.IsContentPack && entry.Manifest.ContentPackFor.UniqueID == "Pathoschild.ContentPatcher"
                                      select entry.Manifest;

            var littleNPCPacks = from pack in contentPatcherPacks
                                 from dependency in pack.Dependencies
                                 where dependency.UniqueID == "Candidus42.LittleNPCs"
                                 select pack.UniqueID;

            if (!littleNPCPacks.Any()) {
                this.Monitor.Log("Could not find a content pack for LittleNPCs. Your LittleNPCs will look like mere toddlers and don't do much.", LogLevel.Error);
            }

            foreach (var pack in littleNPCPacks) {
                this.Monitor.Log($"[{Common.GetHostTag()}] Found content pack for LittleNPCs: {pack}", LogLevel.Info);
            }

            // Read config.
            config_ = helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.GameLoop.OneSecondUpdateTicking += OnOneSecondUpdateTicking;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Events.Player.Warped += OnWarped;

            HarmonyPatcher.Create(this);
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e) {
            ContentPatcherTokens.Register(this);

            // GenericModConfigMenu support.
            var configMenu = this.Helper.ModRegistry.GetApi<GenericModConfigMenu.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null) {
                return;
            }

            configMenu.Register(this.ModManifest,
                                () => config_ = new ModConfig(),
                                () => this.Helper.WriteConfig(config_));

            configMenu.AddNumberOption(this.ModManifest,
                                       () => config_.AgeWhenKidsAreModified,
                                       (val) => config_.AgeWhenKidsAreModified = val,
                                       () => "Age when kids are modified",
                                       min: 1);

            configMenu.AddBoolOption(this.ModManifest,
                                     () => config_.DoChildrenWander,
                                     (val) => config_.DoChildrenWander = val,
                                     () => "Do children wander");

            configMenu.AddBoolOption(this.ModManifest,
                                     () => config_.DoChildrenHaveCurfew,
                                     (val) => config_.DoChildrenHaveCurfew = val,
                                     () => "Do children have curfew");

            configMenu.AddNumberOption(this.ModManifest,
                                       () => config_.CurfewTime,
                                       (val) => config_.CurfewTime = val,
                                       () => "Curfew time",
                                       min: 1200,
                                       max: 2400,
                                       interval: 100);

            configMenu.AddBoolOption(this.ModManifest,
                                     () => config_.DoChildrenVisitVolcanoIsland,
                                     (val) => config_.DoChildrenVisitVolcanoIsland = val,
                                     () => "Do children visit Volcano Island");
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e) {
            // ATTENTION: OnDayStarted() is too early for child conversion, not all assets are loaded yet.
            // We have to use OnOneSecondUpdateTicking() at 60 ticks after OnDayStarted() instead.
            // The only thing we can do here is putting all children about to convert into bed.
            var farmHouse = Utility.getHomeOfFarmer(Game1.player);
            var convertibleChildren = farmHouse.getChildren().Where(c => c.daysOld.Value >= config_.AgeWhenKidsAreModified);
            if (convertibleChildren.Count() > 2) {
                this.Monitor.Log("There are more than two children, only first and second child will be converted.", LogLevel.Info);
            }

            // Put first and second child about to convert into bed.
            foreach (var child in convertibleChildren) {
                if (Common.IsValidLittleNPCIndex(child.GetChildIndex())) {
                    child.setTilePosition(farmHouse.GetChildBedSpot(child.GetChildIndex()));
                    // Set the original child invisible during the day.
                    child.IsInvisible = true;
                    child.HideShadow = true;
                }
            }

            // Set the counter for OnOneSecondUpdateTicking().
            relativeSeconds_ = 0;
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e) {
            ProvideFallbackAssets(e, 0);
            ProvideFallbackAssets(e, 1);
        }

        private void OnOneSecondUpdateTicking(object sender, OneSecondUpdateTickingEventArgs e) {
            // Run only once per day at 60 ticks after OnDayStarted().
            if (!relativeSeconds_.HasValue || ++relativeSeconds_ != 1) {
                return;
            }

            if (TrackedLittleNPCs.Any()) {
                foreach (var npc in TrackedLittleNPCs) {
                    this.Monitor.Log($"{nameof(TrackedLittleNPCs)} still contains {npc.Key.Name}.", LogLevel.Warn);
                }
                this.Monitor.Log($"{nameof(TrackedLittleNPCs)} is not empty, clearing it.", LogLevel.Error);
                TrackedLittleNPCs.Clear();
            }

            var farmHouse = Utility.getHomeOfFarmer(Game1.player);

            var convertibleChildren = farmHouse.getChildren().Where(c => c.daysOld.Value >= config_.AgeWhenKidsAreModified);

            var npcs = farmHouse.characters;

            var childrenToConvert = new List<Child>();
            foreach (var child in convertibleChildren) {
                // Convert only the first two children.
                if (Common.IsValidLittleNPCIndex(child.GetChildIndex())) {
                    childrenToConvert.Add(child);
                }
                else {
                    this.Monitor.Log($"[{Common.GetHostTag()}] Skipping child {child.Name}.", LogLevel.Info);
                }
            }
            foreach (var child in childrenToConvert) {
                var littleNPC = LittleNPC.FromChild(child, farmHouse);
                    // Replace Child by LittleNPC object.
                    npcs.Add(littleNPC);

                    // Copy friendship data.
                    if (Game1.player.friendshipData.TryGetValue(child.Name, out var friendship)) {
                        Game1.player.friendshipData[littleNPC.Name] = friendship;
                        // Removing friendship data removes the child from the social page which is exactly whet we want.
                        Game1.player.friendshipData.Remove(child.Name);
                    }

                    // Add to tracking list.
                    TrackedLittleNPCs[littleNPC] = child;

                    this.Monitor.Log($"[{Common.GetHostTag()}] Added LittleNPC {littleNPC.Name}, deactivated child {child.Name}.", LogLevel.Info);
            }

            if (config_.DoChildrenVisitVolcanoIsland) {
                // Add random island schedule.
                AddRandomIslandSchedule(TrackedLittleNPCs.Keys.ToList());
            }
        }

        private void OnSaving(object sender, SavingEventArgs e) {
            // Only convert items in our tracking list.
            foreach (var item in TrackedLittleNPCs) {
                var littleNPC = item.Key;
                var child = item.Value;

                this.Monitor.Log($"[{Common.GetHostTag()}] ConvertLittleNPCsToChildren: {littleNPC.Name}", LogLevel.Info);

                // Put hat on (part of the save game).
                if (littleNPC.WrappedChildHat is not null) {
                    child.hat.Value = littleNPC.WrappedChildHat;
                }

                // Copy friendship data.
                if (Game1.player.friendshipData.TryGetValue(littleNPC.Name, out var friendship)) {
                    Game1.player.friendshipData[child.Name] = friendship;
                    // Remove friendship data to avoid multiple social page entries.
                    Game1.player.friendshipData.Remove(littleNPC.Name);
                }

                // Set child visible before saving.
                child.IsInvisible = false;

                // Remove from game.
                bool success = false;
                Utility.ForEachLocation(location => {
                    var guidsToRemove = (from c in location.characters
                                         where c.Name == littleNPC.Name
                                         select location.characters.GuidOf(c)).ToList();

                    foreach (var guid in guidsToRemove) {
                        location.characters.Remove(guid);
                        this.Monitor.Log($"[{Common.GetHostTag()}] Removed LittleNPC {littleNPC.Name} in {littleNPC.currentLocation.Name}, reactivated child {child.Name}.", LogLevel.Info);
                        success = true;
                    }

                    return true;
                });

                if (!success) {
                    this.Monitor.Log($"[{Common.GetHostTag()}] Failed to remove LittleNPC {littleNPC.Name} from tracking list.", LogLevel.Error);
                }
            }

            // Clear tracking list.
            TrackedLittleNPCs.Clear();

            relativeSeconds_ = null;
        }

        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e) {
            // Forward the call.
            OnSaving(sender, null);
        }

        /// <summary>
        /// Add island schedule randomly.
        /// </summary>
        /// <param name="littleNPCs"></param>
        private void AddRandomIslandSchedule(List<LittleNPC> littleNPCs) {
            // ATTENTION: CustomNPCExclusions patches the very same methods we'd have to patch,
            // IslandSouth.CanVisitIslandToday() and IslandSouth.SetupIslandSchedules() in a conflicting way.
            // To avoid that we just copied the important parts from IslandSouth.SetupIslandSchedules().
            if (Utility.isFestivalDay(Game1.Date.DayOfMonth, Game1.Date.Season)
             || (Game1.Date.Season == Season.Winter && Game1.Date.DayOfMonth >= 15 && Game1.Date.DayOfMonth <= 17)) {
                return;
            }
            IslandSouth islandSouth = Game1.getLocationFromName("IslandSouth") as IslandSouth;
            if (islandSouth is null || !islandSouth.resortRestored.Value || Game1.IsRainingHere(islandSouth) || !islandSouth.resortOpenToday.Value) {
                return;
            }

            var islandActivityAssignments = new List<IslandSouth.IslandActivityAssigments>();
            var last_activity_assignments = new Dictionary<Character, string>();
            var random = new Random((int) (Game1.uniqueIDForThisGame * 1.21f) + (int) (Game1.stats.DaysPlayed * 2.5f));

            var npcs = littleNPCs.Cast<NPC>().ToList();
            islandActivityAssignments.Add(new IslandSouth.IslandActivityAssigments(1200, npcs, random, last_activity_assignments));
            islandActivityAssignments.Add(new IslandSouth.IslandActivityAssigments(1400, npcs, random, last_activity_assignments));
            islandActivityAssignments.Add(new IslandSouth.IslandActivityAssigments(1600, npcs, random, last_activity_assignments));

            foreach (NPC npc in npcs) {
                if (random.NextDouble() < 0.4) {
                    StringBuilder sb = new StringBuilder();
                    bool hasIslandAttire = IslandSouth.HasIslandAttire(npc);

                    if (hasIslandAttire) {
                        Point dressingRoomPoint = IslandSouth.GetDressingRoomPoint(npc);
                        sb.Append($"/a1150 IslandSouth {dressingRoomPoint.X} {dressingRoomPoint.Y} change_beach");

                        foreach (IslandSouth.IslandActivityAssigments activity in islandActivityAssignments) {
                            string text = activity.GetScheduleStringForCharacter(npc);
                            if (!string.IsNullOrEmpty(text)) {
                                sb.Append(text);
                            }
                        }

                        Point dressingRoomPoint2 = IslandSouth.GetDressingRoomPoint(npc);
                        sb.Append($"/a1730 IslandSouth {dressingRoomPoint2.X} {dressingRoomPoint2.Y} change_normal");

                    }
                    else {
                        bool endActivity = false;
                        foreach (IslandSouth.IslandActivityAssigments activity in islandActivityAssignments) {
                            string text = activity.GetScheduleStringForCharacter(npc);
                            if (!string.IsNullOrEmpty(text)) {
                                if (!endActivity) {
                                    text = $"/a{text.Substring(1)}";
                                    endActivity = true;
                                }
                                sb.Append(text);
                            }
                        }
                    }

                    sb.Append("/1800 bed");

                    sb.Remove(0, 1);
                    if (npc.TryLoadSchedule("island", sb.ToString())) {
                        npc.islandScheduleName.Value = "island";
                        Game1.netWorldState.Value.IslandVisitors.Add(npc.Name);
                    }

                    this.Monitor.Log($"{npc.Name} will visit Volcano Island today.", LogLevel.Info);
                }
            }
        }

        private void OnWarped(object sender, WarpedEventArgs e) {
            // When children appear in different locations, e.g. on festivals reloadSprite()
            // is called and makes children's shadows visible again. We don't want that.
            foreach (var child in TrackedLittleNPCs.Values) {
                child.HideShadow = true;
            }
        }

        /// <summary>
        /// Provides fallback assets from game content.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="index"></param>
        private void ProvideFallbackAssets(AssetRequestedEventArgs e, int index) {
            // Provide assets for LittleNPCs only.
            if (!e.Name.Name.Contains("LittleNPC")) {
                return;
            }

            var littleNPC = new LittleNPCInfo(index);

            // We also use the sprite texture as portrait but should be good enough as a fallback.
            string spriteTextureName = string.Concat("Characters/Toddler",
                                                     (littleNPC.Gender == Gender.Male) ? "" : "_girl");

            // Fallback dialogue.
            string message = string.Concat("Hi dad! Please install a content pack for me.",
                                           "^Hi mom! Please install a content pack for me.",
                                           "#$e#",
                                           "Look for StardewValley Mod 15152 on nexusmods.com for details.");

            var dialogue = new Dictionary<string, string>() {
                { "Mon", message },
                { "Tue", message },
                { "Wed", message },
                { "Thu", message },
                { "Fri", message },
                { "Sat", message },
                { "Sun", message }
            };

            string prefix = Common.PrefixFromChildIndex(index);

            LoadSpriteSheet(e, $"Characters/{prefix}", spriteTextureName);
            LoadSpriteSheet(e, $"Portraits/{prefix}", spriteTextureName);

            if (e.Name.StartsWith($"Characters/Dialogue/{prefix}") && IsNonLocalizedAssetName(e.Name)) {
                e.LoadFrom(() => {
                    if (CachedAssets.TryGetValue(e.Name.Name, out var asset)) {
                        ModEntry.monitor_.Log($"[{Common.GetHostTag()}] Providing cached asset {e.Name}", LogLevel.Info);

                        return (Dictionary<string, string>) asset;
                    }

                    ModEntry.monitor_.Log($"[{Common.GetHostTag()}] Providing fallback asset {e.Name}", LogLevel.Info);

                    return dialogue;
                }, AssetLoadPriority.Low);
            }
        }

        /// <summary>
        /// Loads a cached character resp. portrait sheet or provides a fallback.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="assetNamePrefix"></param>
        /// <param name="spriteTextureName"></param>
        /// <returns></returns>
        private void LoadSpriteSheet(AssetRequestedEventArgs e, string assetNamePrefix, string spriteTextureName) {
            if (e.Name.StartsWith(assetNamePrefix) && IsNonLocalizedAssetName(e.Name)) {
                // Fallback assets are loaded with low priority.
                e.LoadFrom(() => {
                    if (CachedAssets.TryGetValue(e.Name.Name, out var asset)) {
                        ModEntry.monitor_.Log($"[{Common.GetHostTag()}] Providing cached asset {e.Name}", LogLevel.Info);

                        return ((LittleNPC.TransferImage) asset).ToTexture(Game1.graphics.GraphicsDevice);
                    }

                    if (e.Name.Name.Contains("_Beach") && !CachedAssets.TryGetValue(e.Name.Name, out _)) {
                        // Use standard asset for beach.
                        string beachTag = "_Beach";
                        int beachTagStart = e.Name.Name.IndexOf(beachTag, StringComparison.Ordinal);
                        string assetName = e.Name.Name.Remove(beachTagStart, beachTag.Length);
                        if (CachedAssets.TryGetValue(assetName, out asset)) {
                            ModEntry.monitor_.Log($"[{Common.GetHostTag()}] Providing cached asset {e.Name}", LogLevel.Info);

                            return ((LittleNPC.TransferImage) asset).ToTexture(Game1.graphics.GraphicsDevice);
                        }
                    }

                    ModEntry.monitor_.Log($"[{Common.GetHostTag()}] Providing fallback asset {e.Name}", LogLevel.Info);

                    // If a portrait is requested we use part of the sprite texture but that should be good enough as a fallback.
                    return Game1.content.Load<Texture2D>(spriteTextureName);
                }, AssetLoadPriority.Low);
            }
        }

        /// <summary>
        /// Gets a LittleNPC by child index.
        /// </summary>
        /// <param name="childIndex"></param>
        /// <returns></returns>
        internal static LittleNPC GetLittleNPC(int childIndex) {
            // The list of LittleNPCs is not sorted by child index, thus we need a query.
            return TrackedLittleNPCs.Keys.FirstOrDefault(c => c.ChildIndex == childIndex);
        }

        /// <summary>
        /// Checks if a given name belongs to a non-localized asset.
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        private static bool IsNonLocalizedAssetName(IAssetName assetName) {
            // We have to check for locale code and international suffix.
            return string.IsNullOrEmpty(assetName.LocaleCode) && !assetName.Name.EndsWith("_international");
        }
    }
}
