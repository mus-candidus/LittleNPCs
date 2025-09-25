using StardewValley;


namespace LittleNPCs.Framework.Patches {
    class Game1AddNPCsPatch {
        /// <summary>
        /// Prefix for <code>Game1.AddNPCs</code>.
        /// Unsets SpawnIfMissing for every NPC that has a LittleNPC internal asset name to prevent ghost NPC copies.
        /// </summary>
        public static void Prefix() {
            foreach (var item in Game1.characterData) {
                if (Common.IsLittleNPCInternalAssetName(item.Key) && item.Value.SpawnIfMissing) {
                    item.Value.SpawnIfMissing = false;
                    ModEntry.monitor_.Log($"Set SpawnIfMissing of {item.Key} to false.");
                }
            }
        }
    }
}
