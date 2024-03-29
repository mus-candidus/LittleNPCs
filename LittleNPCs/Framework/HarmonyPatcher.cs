using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

using HarmonyLib;

using StardewValley;
using StardewValley.Characters;
using StardewValley.Pathfinding;
using StardewValley.Menus;
using System.Linq;


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
            // SocialPage.FindSocialCharacters patch (postfix).
            harmony.Patch(
                original: AccessTools.Method(typeof(SocialPage), nameof(SocialPage.FindSocialCharacters)),
                postfix:  new HarmonyMethod(typeof(Patches.SPFindSocialCharactersPatch), nameof(Patches.SPFindSocialCharactersPatch.Postfix))
            );
        }
    }
}
