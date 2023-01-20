using StardewModdingAPI;

using StardewValley.Characters;


namespace LittleNPCs.Framework.Patches {
    /// <summary>
    /// Prefix for <code>Child.GetChildIndex</code>.
    /// Provides consistent indexing for LittleNPC and Child objects.
    /// </summary>
    public class ChildGetChildIndexPatch {
        public static bool Prefix(Child __instance, ref int __result) {
            if (ModEntry.ChildGetChildIndexPatchEnabled) {
                __result = ModEntry.GetChildIndex(__instance.Name);

                ModEntry.monitor_.Log($"GetChildIndex({__instance.Name}) returned {__result}", LogLevel.Warn);

                // Disable original method.
                return false;
            }
            else {
                // Enable original method.
                return true;
            }
        }
    }
}
