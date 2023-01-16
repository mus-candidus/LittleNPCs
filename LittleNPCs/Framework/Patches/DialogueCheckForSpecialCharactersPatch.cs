using StardewValley;


namespace LittleNPCs.Framework.Patches {
    /// <summary>
    /// Prefix for <code>Dialogue.checkForSpecialCharacters</code>.
    /// Enables use of special variables %kid1 and %kid2 for LittleNPC objects.
    /// </summary>
    class DialogueCheckForSpecialCharactersPatch {
        public static bool Prefix(Dialogue __instance, ref string str) {
            str = str.Replace("%kid1", (ModEntry.LittleNPCNames.Count > 0) ? ModEntry.LittleNPCNames[0] : Game1.content.LoadString("Strings/StringsFromCSFiles:Dialogue.cs.793"));
            str = str.Replace("%kid2", (ModEntry.LittleNPCNames.Count > 1) ? ModEntry.LittleNPCNames[1] : Game1.content.LoadString("Strings/StringsFromCSFiles:Dialogue.cs.794"));

            // Enable original method.
            return true;
        }
    }
}
