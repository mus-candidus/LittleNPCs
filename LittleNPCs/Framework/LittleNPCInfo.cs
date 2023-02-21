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

        public string DisplayName { get; private set; }

        public string Gender { get; private set; }

        public LittleNPCInfo(int childIndex, IMonitor monitor) {
            monitor_ = monitor;
            if (Context.IsWorldReady) {
                var littleNPC = ModEntry.GetLittleNPC(childIndex);
                if (littleNPC is not null) {
                    // Internal asset name of a LittleNPC already has a prefix.
                    Name = littleNPC.Name;
                    DisplayName = littleNPC.displayName;
                    Gender = littleNPC.Gender == 0 ? "male": "female";
                    monitor_.VerboseLog($"GetLittleNPC({childIndex}) returns {Name}");
                }
                else {
                    // No LittleNPC, try to get Child object.
                    var children = GetChildrenFromFarmHouse(false, out FarmHouse farmHouse);
                    Child child = children.FirstOrDefault(c => c.daysOld.Value >= ModEntry.config_.AgeWhenKidsAreModified && c.GetChildIndex() == childIndex);
                    if (child is not null) {
                        // Internal asset name to create a LittleNPC from needs a prefix.
                        string prefix = childIndex == 0 ? "FirstLittleNPC" : "SecondLittleNPC";
                        Name = $"{prefix}{child.Name}";
                        DisplayName = child.Name;
                        Gender = child.Gender == 0 ? "male": "female";
                        monitor_.VerboseLog($"Query for convertible child with index {childIndex} returns {Name}");
                    }
                    else {
                        monitor_.VerboseLog($"Query for convertible child with index {childIndex} returns null");
                    }
                }
            }
            else {
                // World not ready, load from save.
                var children = GetChildrenFromFarmHouse(true, out FarmHouse farmHouse);
                Child child = children.FirstOrDefault(c => c.daysOld.Value >= ModEntry.config_.AgeWhenKidsAreModified && c.GetChildIndex() == childIndex);
                if (child is not null) {
                    // Internal asset name to create a LittleNPC from needs a prefix.
                    string prefix = childIndex == 0 ? "FirstLittleNPC" : "SecondLittleNPC";
                    Name = $"{prefix}{child.Name}";
                    DisplayName = child.Name;
                    Gender = child.Gender == 0 ? "male": "female";
                    monitor_.VerboseLog($"Query for convertible child with index {childIndex} returns {Name}");
                }
                else {
                    monitor_.VerboseLog($"Query for convertible child with index {childIndex} returns null");
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
