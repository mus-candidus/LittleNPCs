using System.Collections.Generic;
using System.Linq;
using StardewValley.Characters;
using StardewValley.Locations;


namespace LittleNPCs.Framework.Patches {
    class UtilPlayersCanGetPregnantHerePatch {
        /// <summary>
        /// Prefix for <code>Utility.playersCanGetPregnantHere</code>.
        /// Changes maximum number of children.
        /// </summary>
        public static bool Prefix(FarmHouse farmHouse, ref bool __result) {
            List<Child> children = farmHouse.getChildren();
            if (farmHouse.cribStyle.Value <= 0) {
                __result = false;

                // Disable original method.
                return false;
            }
            if (farmHouse.getChildrenCount() < Common.MaximumNumberOfLittleNPCs && farmHouse.upgradeLevel >= 2 && children.Count < Common.MaximumNumberOfLittleNPCs) {
                if (children.Count != 0) {
                    // Verify that all children are toddlers.
                    __result = children.All(c => c.Age > Child.crawler);
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