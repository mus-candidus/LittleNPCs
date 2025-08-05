using StardewValley;


namespace LittleNPCs.Framework.Patches {
    /// <summary>
    /// Postfix for <code>GameLocation.cleanupBeforeSave</code>.
    /// Removes all LittleNPCs before saving.
    /// </summary>
    class GLCCleanupBeforeSave {
        public static void Postfix(GameLocation __instance) {
            int removed = __instance.characters.RemoveWhere(npc => npc is LittleNPC);
            if (removed > 0) {
                ModEntry.monitor_.Log($"{nameof(GameLocation.cleanupBeforeSave)} postfix: Removed {removed} LittleNPCs from location {__instance.Name}",
                    StardewModdingAPI.LogLevel.Warn);
            }
        }
    }
}
