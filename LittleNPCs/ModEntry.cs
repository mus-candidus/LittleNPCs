using System;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Locations;

using LittleNPCs.Framework;


namespace LittleNPCs {

    public class ModEntry : Mod {

        public static IModHelper helper_;

        public static IMonitor monitor_;

        public static ModConfig config_;

        // We have to keep track of LittleNPCs for various reasons.
        public static List<LittleNPC> LittleNPCsList { get; } = new List<LittleNPC>();

        public override void Entry(IModHelper helper) {
            ModEntry.helper_ = helper;
            monitor_ = this.Monitor;

            // Read config.
            config_ = helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.GameLoop.ReturnedToTitle += (sender, e) => { LittleNPCsList.Clear(); };

            // Create Harmony instance.
            Harmony harmony = new Harmony(this.ModManifest.UniqueID);
            // NPC.arriveAtFarmHouse (postfix).
            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), nameof(NPC.arriveAtFarmHouse)),
                postfix: new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.NPCArriveAtFarmHousePatch), nameof(LittleNPCs.Framework.Patches.NPCArriveAtFarmHousePatch.Postfix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), nameof(NPC.checkSchedule)),
                prefix: new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.NPCCheckSchedulePatch), nameof(LittleNPCs.Framework.Patches.NPCCheckSchedulePatch.Prefix))
            );
            // NPC.parseMasterSchedule patch (prefix, postfix, finalizer).
            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), nameof(NPC.parseMasterSchedule)),
                prefix: new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.NPCParseMasterSchedulePatch), nameof(LittleNPCs.Framework.Patches.NPCParseMasterSchedulePatch.Prefix)),
                postfix: new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.NPCParseMasterSchedulePatch), nameof(LittleNPCs.Framework.Patches.NPCParseMasterSchedulePatch.Postfix)),
                finalizer: new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.NPCParseMasterSchedulePatch), nameof(LittleNPCs.Framework.Patches.NPCParseMasterSchedulePatch.Finalizer))
            );
            // NPC.prepareToDisembarkOnNewSchedulePath patch (postfix).
            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), "prepareToDisembarkOnNewSchedulePath"),
                postfix: new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.NPCPrepareToDisembarkOnNewSchedulePathPatch), nameof(LittleNPCs.Framework.Patches.NPCPrepareToDisembarkOnNewSchedulePathPatch.Postfix))
            );
            // PathFindController.handleWarps patch (prefix).
            harmony.Patch(
                original: AccessTools.Method(typeof(PathFindController), nameof(PathFindController.handleWarps)),
                prefix: new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.PFCHandleWarpsPatch), nameof(LittleNPCs.Framework.Patches.PFCHandleWarpsPatch.Prefix))
            );
            // Dialogue.checkForSpecialCharacters patch (prefix).
            harmony.Patch(
                original: AccessTools.Method(typeof(Dialogue), nameof(Dialogue.checkForSpecialCharacters)),
                prefix: new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.DialogueCheckForSpecialCharactersPatch), nameof(LittleNPCs.Framework.Patches.DialogueCheckForSpecialCharactersPatch.Prefix))
            );
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e) {
            var farmHouse = Utility.getHomeOfFarmer(Game1.player);

            // Getting child indices must be done before removing any child.
            var childIndexMap = farmHouse.getChildren()
                                         .ToDictionary(c => c,
                                                       c => c.GetChildIndex());

            var npcs = farmHouse.characters;

            // Plain old for loop because we have to replace list elements.
            for (int i = 0; i < npcs.Count; ++i) {
                if (npcs[i] is Child child) {
                    var littleNPC = LittleNPC.FromChild(child, childIndexMap[child], farmHouse, this.Monitor);
                    // Replace Child by LittleNPC object.
                    npcs[i] = littleNPC;

                    // Add to tracking list.
                    LittleNPCsList.Add(littleNPC);

                    this.Monitor.Log($"Replaced child {child.Name} by LittleNPC, default position {Utility.Vector2ToPoint(child.Position / 64f)}", LogLevel.Warn);
                }
            }
        }

        private void OnSaving(object sender, SavingEventArgs e) {
            // Local function, only needed here.
            void ConvertLittleNPCsToChildren(Netcode.NetCollection<NPC> npcs) {
                // Plain old for-loop because we have to replace list elements.
                for (int i = 0; i < npcs.Count; ++i) {
                    if (npcs[i] is LittleNPC littleNPC) {
                        var child = littleNPC.WrappedChild;
                        // ATTENTION: By removing children we prevent them from aging properly so we have call dayUpdate() explicitly.
                        child.dayUpdate(Game1.dayOfMonth);
                        // Replace LittleNPC by Child object.
                        npcs[i] = child;

                        // Remove from tracking list.
                        LittleNPCsList.Remove(littleNPC);

                        this.Monitor.Log($"Replaced LittleNPC in {npcs[i].currentLocation.Name} by child {child.Name}", LogLevel.Warn);
                    }
                }
            }

            // ATTENTION: Avoid Utility.getAllCharacters(), replacing elements in the returned list doesn't work.
            // We have to iterate over all locations instead.

            // Check outdoor locations and convert LittleNPCs back if necessary.
            foreach (GameLocation location in Game1.locations) {
                // Plain old for-loop because we have to replace list elements.
                var npcs = location.characters;
                ConvertLittleNPCsToChildren(npcs);
            }

            // Check indoor locations and convert LittleNPCs back if necessary.
            foreach (BuildableGameLocation location in Game1.locations.OfType<BuildableGameLocation>()) {
                foreach (Building building in location.buildings) {
                    if (building.indoors.Value is not null) {
                        var npcs = building.indoors.Value.characters;
                        ConvertLittleNPCsToChildren(npcs);
                    }
                }
            }

            Assert(!LittleNPCsList.Any(), $"{nameof(LittleNPCsList)} is not empty");
        }

        public static long GetFarmerParentId(Character npc) {
            return (npc is LittleNPC littleNPC) ? littleNPC.WrappedChild.idOfParent.Value : 0; 
        }

        /// <summary>
        /// Custom assert method because <code>Debug.Assert()</code> takes the whole application down. 
        /// </summary>
        private static void Assert(bool condition, string message) {
            if (!condition) {
                throw new InvalidOperationException(message);
            }
        }
    }
}
