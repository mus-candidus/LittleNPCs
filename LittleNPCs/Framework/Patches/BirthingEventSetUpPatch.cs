using System;

using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;


namespace LittleNPCs.Framework.Patches {
    class BirthingEventSetUpPatch {
        /// <summary>
        /// Prefix for <code>BirthingEvent.setUp</code>.
        /// Ensures that genders of additional children are randomly chosen.
        /// </summary>
        public static bool Prefix(ref bool __result, ref bool ___isMale, ref string ___message) {
            Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame, Game1.stats.DaysPlayed);
            NPC npc = Game1.RequireCharacter(Game1.player.spouse);
            Game1.player.CanMove = false;

            if (Game1.player.getNumberOfChildren() == 1) {
                // Second child always has a gender opposite to first child.
                ___isMale = Game1.player.getChildren()[0].Gender == Gender.Female;
            }
            else {
                ___isMale = random.NextBool();
            }

            if (npc.isAdoptionSpouse()) {
                ___message = Game1.content.LoadString("Strings\\Events:BirthMessage_Adoption", Lexicon.getGenderedChildTerm(___isMale));
            }
            else if (npc.Gender == Gender.Male) {
                ___message = Game1.content.LoadString("Strings\\Events:BirthMessage_PlayerMother", Lexicon.getGenderedChildTerm(___isMale));
            }
            else {
                ___message = Game1.content.LoadString("Strings\\Events:BirthMessage_SpouseMother", Lexicon.getGenderedChildTerm(___isMale), npc.displayName);
            }

            __result = false;

            // Disable original method.
            return false;
        }
    }
}
