using HarmonyLib;

using StardewValley;


namespace LittleNPCs.Framework {
    internal static class HarmonyPatcher {
        public static void Create(ModEntry modEntry) {
            // Create Harmony instance.
            Harmony harmony = new Harmony(modEntry.ModManifest.UniqueID);
            // NPC.arriveAtFarmHouse (postfix).
            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), nameof(NPC.arriveAtFarmHouse)),
                postfix:  new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.NPCArriveAtFarmHousePatch), nameof(LittleNPCs.Framework.Patches.NPCArriveAtFarmHousePatch.Postfix))
            );
            // NPC.checkSchedule patch (prefix).
            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), nameof(NPC.checkSchedule)),
                prefix:   new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.NPCCheckSchedulePatch), nameof(LittleNPCs.Framework.Patches.NPCCheckSchedulePatch.Prefix))
            );
            // NPC.parseMasterSchedule patch (prefix, postfix, finalizer).
            harmony.Patch(
                original:  AccessTools.Method(typeof(NPC), nameof(NPC.parseMasterSchedule)),
                prefix:    new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.NPCParseMasterSchedulePatch), nameof(LittleNPCs.Framework.Patches.NPCParseMasterSchedulePatch.Prefix)),
                postfix:   new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.NPCParseMasterSchedulePatch), nameof(LittleNPCs.Framework.Patches.NPCParseMasterSchedulePatch.Postfix)),
                finalizer: new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.NPCParseMasterSchedulePatch), nameof(LittleNPCs.Framework.Patches.NPCParseMasterSchedulePatch.Finalizer))
            );
            // NPC.prepareToDisembarkOnNewSchedulePath patch (postfix).
            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), "prepareToDisembarkOnNewSchedulePath"),
                postfix:  new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.NPCPrepareToDisembarkOnNewSchedulePathPatch), nameof(LittleNPCs.Framework.Patches.NPCPrepareToDisembarkOnNewSchedulePathPatch.Postfix))
            );
            // PathFindController.handleWarps patch (prefix).
            harmony.Patch(
                original: AccessTools.Method(typeof(PathFindController), nameof(PathFindController.handleWarps)),
                prefix:   new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.PFCHandleWarpsPatch), nameof(LittleNPCs.Framework.Patches.PFCHandleWarpsPatch.Prefix))
            );
            // Dialogue.checkForSpecialCharacters patch (prefix).
            harmony.Patch(
                original: AccessTools.Method(typeof(Dialogue), nameof(Dialogue.checkForSpecialCharacters)),
                prefix:   new HarmonyMethod(typeof(LittleNPCs.Framework.Patches.DialogueCheckForSpecialCharactersPatch), nameof(LittleNPCs.Framework.Patches.DialogueCheckForSpecialCharactersPatch.Prefix))
            );
        }
    }
}