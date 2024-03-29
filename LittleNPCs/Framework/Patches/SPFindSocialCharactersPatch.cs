using System.Collections.Generic;

using StardewValley.Characters;
using StardewValley.Menus;


namespace LittleNPCs.Framework.Patches {
    /// <summary>
    /// Postfix for <code>SocialPage.FindSocialCharacters</code>.
    /// Removes hidden children from social page.
    /// </summary>
    class SPFindSocialCharactersPatch {
        public static void Postfix(SocialPage __instance, ref List<SocialPage.SocialEntry> __result) {
            // Filter out invisible children.
            __result.RemoveAll(e => e.Character is Child c && c.IsInvisible);
        }
    }
}
