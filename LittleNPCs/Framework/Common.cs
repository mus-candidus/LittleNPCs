using System;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Characters;


namespace LittleNPCs.Framework {
    internal static class Common {
        /// <summary>
        /// Returns maximum number of LittleNPCs.
        /// </summary>
        public static int MaximumNumberOfLittleNPCs => 2;

        /// <summary>
        /// Returns a tag to identify host or client for multiplayer games.
        /// </summary>
        /// <returns></returns>
        public static string GetHostTag() {
            return Game1.IsMasterGame ? "Host" : "Client";
        }

        /// <summary>
        /// Creates a unique internal name from child index and name.
        /// </summary>
        /// <param name="childIndex"></param>
        /// <param name="childName"></param>
        /// <returns></returns>
        public static string CreateInternalAssetName(int childIndex, string childName) {
            // Internal asset name to create a LittleNPC from needs a prefix.
            string prefix = PrefixFromChildIndex(childIndex);

            // Remove spaces. This could to equal names but there's still the prefix to distinguish them.
            string sanitizedChildName = childName.Replace(' ', '_');

            return $"{prefix}{sanitizedChildName}{Game1.player.UniqueMultiplayerID}";
        }

        /// <summary>
        /// Determines internal asset name prefix from child index.
        /// </summary>
        /// <param name="childIndex"></param>
        /// <returns></returns>
        public static string PrefixFromChildIndex(int childIndex) {
            return childIndex == 0 ? "FirstLittleNPC" : "SecondLittleNPC";
        }

        /// <summary>
        /// Returns the birthday of the given child.
        /// </summary>
        /// <param name="child"></param>
        /// <param name="loadFromSave"></param>
        /// <returns></returns>
        public static SDate GetBirthday(Child child, bool loadFromSave) {
            SDate today = (loadFromSave && SaveGame.loaded is not null)
                ? new SDate(SaveGame.loaded.dayOfMonth, SaveGame.loaded.currentSeason, SaveGame.loaded.year)
                : SDate.Now();

            SDate birthday;
            try {
                // Subtract age of child in days from current date.
                birthday = today.AddDays(-child.daysOld.Value);
            }
            catch (ArithmeticException) {
                // Fallback.
                birthday = new SDate(1, "spring");
            }

            return birthday;
        }

        /// <summary>
        /// Checks if child index is a valid LittleNPC index.
        /// </summary>
        /// <param name="childIndex"></param>
        /// <returns></returns>
        public static bool IsValidLittleNPCIndex(int childIndex) {
            // Only the first two children can be converted.
            return (childIndex == 0 || childIndex == 1);
        }
    }
}