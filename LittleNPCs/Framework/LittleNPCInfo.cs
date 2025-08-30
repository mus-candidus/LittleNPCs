using System.Collections.Generic;
using System.Linq;

using StardewModdingAPI;
using StardewModdingAPI.Utilities;

using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;


namespace LittleNPCs.Framework {
    internal record LittleNPCInfo {
        public enum LoadState { None, LittleNPC, Child, SaveGame }

        public string Name { get; private set; }

        public string DisplayName { get; private set; }

        public Gender Gender { get; private set; }

        public SDate Birthday { get; private set; }

        public LoadState LoadedFrom { get; private set; }

        public LittleNPCInfo(int childIndex) {
            if (Context.IsWorldReady) {
                var littleNPC = ModEntry.GetLittleNPC(childIndex);
                if (littleNPC is not null) {
                    // Internal asset name of a LittleNPC already has a prefix.
                    Name = littleNPC.Name;
                    DisplayName = littleNPC.displayName;
                    Gender = littleNPC.Gender;
                    Birthday = littleNPC.GetBirthday();
                    LoadedFrom = LoadState.LittleNPC;
                    ModEntry.monitor_.VerboseLog($"[{Common.GetHostTag()}] GetLittleNPC({childIndex}) returns {this}");
                }
                else {
                    // No LittleNPC, try to get Child object.
                    AssignFromChild(this, false, childIndex);
                }
            }
            else {
                // World not ready, load from save.
                AssignFromChild(this, true, childIndex);
            }
        }

        private static void AssignFromChild(LittleNPCInfo info, bool loadFromSave, int childIndex) {
            var children = GetChildrenFromFarmHouse(loadFromSave, out FarmHouse farmHouse);
            Child child = children.FirstOrDefault(c => c.daysOld.Value >= ModEntry.config_.AgeWhenKidsAreModified && c.GetChildIndex() == childIndex);
            if (child is not null) {
                info.Name = Common.CreateInternalAssetName(childIndex, child.Name);
                info.DisplayName = child.Name;
                info.Gender = child.Gender;
                info.Birthday = Common.GetBirthday(child, loadFromSave);
                info.LoadedFrom = LoadState.Child;
                ModEntry.monitor_.VerboseLog($"[{Common.GetHostTag()}] Query for convertible child with index {childIndex} returns {info}");
            }
            else {
                ModEntry.monitor_.VerboseLog($"[{Common.GetHostTag()}] Query for convertible child with index {childIndex} returns null");
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
