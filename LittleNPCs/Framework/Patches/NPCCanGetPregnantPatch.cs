using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;


namespace LittleNPCs.Framework.Patches {
    class NPCCanGetPregnantPatch {
        /// <summary>
        /// Prefix for <code>NPC.canGetPregnant</code>.
        /// Changes maximum number of children.
        /// </summary>
        public static bool Prefix(NPC __instance, ref bool __result) {
            if (__instance is Horse || __instance.Name.Equals("Krobus") || __instance.isRoommate() || __instance.IsInvisible) {
                __result = false;

                // Disable original method.
                return false;
            }

            Farmer spouse = __instance.getSpouse();
            if (spouse is null || spouse.divorceTonight.Value) {
                __result = false;

                // Disable original method.
                return false;
            }

            int friendshipHeartLevelForNPC = spouse.getFriendshipHeartLevelForNPC(__instance.Name);
            Friendship spouseFriendship = spouse.GetSpouseFriendship();
            List<Child> children = spouse.getChildren();
            __instance.DefaultMap = spouse.homeLocation.Value;
            FarmHouse homeOfFarmer = Utility.getHomeOfFarmer(spouse);
            if (homeOfFarmer.cribStyle.Value <= 0) {
                __result = false;

                // Disable original method.
                return false;
            }

            if (homeOfFarmer.upgradeLevel >= 2 && spouseFriendship.DaysUntilBirthing < 0 && friendshipHeartLevelForNPC >= 10 && spouse.GetDaysMarried() >= 7) {
                if (children.Count != 0) {
                    // Compare to maximum number of children and verify that all of them are toddlers.
                    __result = (children.Count < ModEntry.config_.MaximumNumberOfChildren && children.All(c => c.Age > Child.crawler));
                }
                else {
                    __result = true;
                }
            }
            else {
                __result = false;
            }

            // Disable original method.
            return false;
        }
    }
}
