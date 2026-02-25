using System.Linq;

namespace LittleNPCs.Framework {
    public class LittleNPCsAPI : ILittleNPCsAPI {
        /// <inheritdoc />
        public bool IsValidLittleNPCIndex(int childIndex) => Common.IsValidLittleNPCIndex(childIndex);

        /// <inheritdoc />
        public int DaysAfterKidsGrowUp => ModEntry.config_.AgeWhenKidsAreModified;
    }
}
