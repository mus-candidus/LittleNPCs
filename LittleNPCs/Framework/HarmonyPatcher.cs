using HarmonyLib;

using StardewValley;
using StardewValley.Characters;


namespace LittleNPCs.Framework {
    internal static class HarmonyPatcher {
        public static void Create(ModEntry modEntry) {
            // Create Harmony instance.
            Harmony harmony = new Harmony(modEntry.ModManifest.UniqueID);
            // PathFindController.handleWarps patch (prefix).
            harmony.Patch(
                original: AccessTools.Method(typeof(PathFindController), nameof(PathFindController.handleWarps)),
                prefix:   new HarmonyMethod(typeof(Patches.PFCHandleWarpsPatch), nameof(Patches.PFCHandleWarpsPatch.Prefix))
            );
            // Dialogue.ReplacePlayerEnteredStrings patch (prefix).
            harmony.Patch(
                original: AccessTools.Method(typeof(Dialogue), nameof(Dialogue.ReplacePlayerEnteredStrings)),
                prefix:   new HarmonyMethod(typeof(Patches.ReplacePlayerEnteredStringsPatch), nameof(Patches.ReplacePlayerEnteredStringsPatch.Prefix))
            );
            // Child.GetChildIndex patch (prefix).
            harmony.Patch(
                original: AccessTools.Method(typeof(Child), nameof(Child.GetChildIndex)),
                prefix:   new HarmonyMethod(typeof(Patches.ChildGetChildIndexPatch), nameof(Patches.ChildGetChildIndexPatch.Prefix))
            );
        }
    }
}
