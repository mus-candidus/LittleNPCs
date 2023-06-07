using System;
using Microsoft.Xna.Framework.Graphics;

using HarmonyLib;

using StardewValley;
using StardewValley.Characters;
using StardewValley.Pathfinding;


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
            // Child.checkAction patch (prefix).
            harmony.Patch(
                original: AccessTools.Method(typeof(Child), nameof(Child.checkAction)),
                prefix:   new HarmonyMethod(typeof(Patches.ChildCheckActionPatch), nameof(Patches.ChildCheckActionPatch.Prefix))
            );
        }
    }
}
