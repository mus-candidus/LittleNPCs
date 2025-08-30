using HarmonyLib;

using StardewValley;
using StardewValley.Pathfinding;
using StardewValley.Menus;

using LittleNPCs.Framework.Patches;


namespace LittleNPCs.Framework {
    internal static class HarmonyPatcher {
        public static void Create(ModEntry modEntry) {
            // Create Harmony instance.
            Harmony harmony = new Harmony(modEntry.ModManifest.UniqueID);
            // PathFindController.handleWarps patch (prefix).
            harmony.Patch(
                original: AccessTools.Method(typeof(PathFindController), nameof(PathFindController.handleWarps)),
                prefix:   new HarmonyMethod(typeof(PFCHandleWarpsPatch), nameof(PFCHandleWarpsPatch.Prefix))
            );
            // SocialPage.FindSocialCharacters patch (postfix).
            harmony.Patch(
                original: AccessTools.Method(typeof(SocialPage), nameof(SocialPage.FindSocialCharacters)),
                postfix:  new HarmonyMethod(typeof(SPFindSocialCharactersPatch), nameof(SPFindSocialCharactersPatch.Postfix))
            );
            // GameLocation.cleanupBeforeSave patch (postfix).
            harmony.Patch(
                original:AccessTools.Method(typeof(GameLocation), nameof(GameLocation.cleanupBeforeSave)),
                postfix:  new HarmonyMethod(typeof(GLCCleanupBeforeSave), nameof(GLCCleanupBeforeSave.Postfix))
            );
        }
    }
}
