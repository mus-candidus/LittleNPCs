
namespace LittleNPCs.Framework {
    /// <summary>
    /// ModConfig Options:
    /// AgeInDaysWhenChildrenBecomeLittleNPCs: The age in days when a child is replaced by a LittleNPC. Default is 83 days.
    /// DoChildrenRunAroundInTheHouse: If true, children run around in the house every hour unless they have a schedule.
    /// DoChildrenHaveCurfew: If true, children will head home at curfew time.
    /// CurfewTime: The time of curfew when DoChildrenHaveCurfew is true. Default is 1900 (7PM).
    /// DoChildrenVisitVolcanoIsland: Children visit Volcano Island by chance. Default is false.
    /// MaximumNumberOfChildren: Maximum number of children. Default is 4.
    /// MaximumNumberOfLittleNPCs: Maximum number of children that become LittleNPCs. Default is 4.
    /// </summary>
    public record class ModConfig {
        public int AgeInDaysWhenChildrenBecomeLittleNPCs { get; set; } = 83;
        public bool DoChildrenRunAroundInTheHouse { get; set; } = true;
        public bool DoChildrenHaveCurfew { get; set; } = true;
        public int CurfewTime { get; set; } = 1900;
        public bool DoChildrenVisitVolcanoIsland { get; set; }
        public int MaximumNumberOfChildren { get; set; } = 4;
        public int MaximumNumberOfLittleNPCs { get; set; } = 4;
    }
}
