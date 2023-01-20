using System;
using System.Collections.Generic;
using System.Linq;

using StardewModdingAPI;

using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;


namespace LittleNPCs.Framework {
    internal class LittleNPCInfo {
        private IMonitor monitor_;

        public string Name { get; private set; }

        public string Gender { get; private set; }

        public LittleNPCInfo(int childIndex, IMonitor monitor) {
            monitor_ = monitor;
            if (Context.IsWorldReady) {
                var littleNPC = ModEntry.GetLittleNPC(childIndex);
                if (littleNPC is not null) {
                    Name = littleNPC.Name;
                    Gender = littleNPC.Gender == 0 ? "male": "female";
                    monitor_.Log($"GetLittleNPC({childIndex}) returns {Name}");
                }
                else {
                    // No LittleNPC, try to get Child object.
                    monitor_.Log($"GetLittleNPC({childIndex}) returns null");
                    var children = GetChildrenFromFarmHouse(false, out FarmHouse farmHouse);
                    Child child = children.FirstOrDefault(c => c.daysOld.Value >= ModEntry.config_.AgeWhenKidsAreModified && c.GetChildIndex() == childIndex);
                    if (child is not null) {
                        Name = child.Name;
                        Gender = child.Gender == 0 ? "male": "female";
                        monitor_.Log($"Query for child with {childIndex} returns {Name}");
                    }
                    else {
                        monitor_.Log($"Query for child with {childIndex} returns null");
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
                        monitor_.Log($"Query for child with {childIndex} returns {Name}");
                    }
                    else {
                        monitor_.Log($"Query for child with {childIndex} returns null");
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
