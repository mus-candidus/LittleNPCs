using System;
using System.Collections.Generic;
using System.Linq;

using StardewModdingAPI;

using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;


namespace LittleNPCs.Framework {
    internal class LittleNPCInfo {
        public string Name { get; private set; }

        public string Gender { get; private set; }

        public LittleNPCInfo(int childIndex) {
            if (Context.IsWorldReady) {
                var littleNPC = ModEntry.GetLittleNPC(childIndex);
                if (littleNPC is not null) {
                    Name = littleNPC.Name;
                    Gender = littleNPC.Gender == 0 ? "male": "female";
                    ModEntry.monitor_.Log($"GetLittleNPC({childIndex}) returned {Name}", LogLevel.Warn);
                }
                else {
                    // No LittleNPC, try to get Child object.
                    ModEntry.monitor_.Log($"GetLittleNPC({childIndex}) returned null", LogLevel.Warn);
                    var children = GetChildrenFromFarmHouse(false, out FarmHouse farmHouse);
                    Child child = children.FirstOrDefault(c => c.daysOld.Value >= ModEntry.config_.AgeWhenKidsAreModified && c.GetChildIndex() == childIndex);
                    if (child is not null) {
                        Name = child.Name;
                        Gender = child.Gender == 0 ? "male": "female";
                        ModEntry.monitor_.Log($"getChildren().Name returned {Name}", LogLevel.Warn);
                    }
                }
            }
            else {
                // World not ready, load from save.
                var children = GetChildrenFromFarmHouse(true, out FarmHouse farmHouse);
                Child child = children.FirstOrDefault(c => c.daysOld.Value >= ModEntry.config_.AgeWhenKidsAreModified && c.GetChildIndex() == childIndex);
                if (child is not null) {
                    Name = child.Name;
                    Gender = child.Gender == 0 ? "male": "female";
                    ModEntry.monitor_.Log($"Save.getChildren().Name returned {Name}", LogLevel.Warn);
                }
            }
        }

        private static IEnumerable<Child> GetChildrenFromFarmHouse(bool loadFromSave, out FarmHouse farmHouse) {
            farmHouse = loadFromSave ? SaveGame.loaded?.locations.OfType<FarmHouse>().FirstOrDefault(l => l.Name == "FarmHouse")
                                     : Utility.getHomeOfFarmer(Game1.player);
            
            return farmHouse is not null ? farmHouse.getChildren()
                                         : Enumerable.Empty<Child>();
        }
    }
}
