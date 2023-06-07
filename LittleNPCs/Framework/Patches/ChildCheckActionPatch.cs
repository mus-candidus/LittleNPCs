using StardewValley;
using StardewValley.Characters;


namespace LittleNPCs.Framework.Patches {
    /// <summary>
    /// Prefix for <code>Child.checkAction</code>.
    /// Disables interaction with children.
    /// </summary>
    public class ChildCheckActionPatch {
        public static bool Prefix(Child __instance, Farmer who, GameLocation l, ref bool __result) {
            if (__instance.daysOld.Value >= ModEntry.config_.AgeWhenKidsAreModified) {
                __result = false;

                // Disable original method.
                return false;
            }
            
            // Enable original method.
            return true;
        }
    }
}
