using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;

using StardewModdingAPI;

using LittleNPCs;
using LittleNPCs.Framework;


namespace LittleNPCs.Framework.Patches
{
    /* Prefix for performTenMinuteUpdate
     * Normally, performTenMinuteUpdate just handles the dialogue bubble while walking.
     * I've combined this with code from Child.tenMinuteUpdate to imitate Child behavior.
     * Children will wander around the house every hour.
     * I've also added a curfew system, so children go to bed at the (configurable) curfew time when at home.
     */
    class NPCPerformTenMinuteUpdatePatch
    {
        public static bool Prefix(NPC __instance)
        {
            if (!(__instance is LittleNPC) || !Game1.IsMasterGame)
                return true;

            ModEntry.monitor_.Log($"{__instance.Name} {__instance.currentLocation}, {Utility.Vector2ToPoint(__instance.Position / 64f)}", LogLevel.Warn);

            FarmHouse farmHouse = Utility.getHomeOfFarmer(Game1.player);
            if (farmHouse.characters.Contains(__instance))
            {
                ModConfig config = ModEntry.config_;
                //Send children to bed when inside home
                if (config.DoChildrenHaveCurfew && Game1.timeOfDay == config.CurfewTime)
                {
                    __instance.IsWalkingInSquare = false;
                    __instance.Halt();
                    __instance.temporaryController = null;

                    //Child is at home, directly path to bed (DefaultPosition)
                    Point bedPoint = new Point((int)__instance.DefaultPosition.X / 64, (int)__instance.DefaultPosition.Y / 64);
                    __instance.controller = new PathFindController(__instance, farmHouse, bedPoint, 2);

                    if (__instance.controller.pathToEndPoint == null || !farmHouse.isTileOnMap(__instance.controller.pathToEndPoint.Last().X, __instance.controller.pathToEndPoint.Last().Y))
                        __instance.controller = null;
                }
                //Make children wander if they have nothing better to do
                // ATTENTION: We have to skip that for scheduled times, otherwise schedule and random wandering overlap in a weird way:
                // The NPCs get warped out of farm house before they reach their random destination points in the house
                // and thus are doomed to walk around in the BusStop location endlessly without a chance to reach their destination!
                else if (__instance.controller == null
                         && config.DoChildrenWander
                         && (__instance.Schedule == null || !__instance.Schedule.ContainsKey(Game1.timeOfDay))
                         && Game1.timeOfDay % 100 == 0
                         && Game1.timeOfDay < config.CurfewTime)
                {
                    if (!__instance.currentLocation.Equals(Utility.getHomeOfFarmer(Game1.player)))
                        return true;

                    __instance.IsWalkingInSquare = false;
                    __instance.Halt();

                    //If I'm going to prevent them from wandering into doorways, I need to do it here.
                    __instance.controller = new PathFindController(__instance, farmHouse, farmHouse.getRandomOpenPointInHouse(Game1.random, 0, 30), 2);
                    if (__instance.controller.pathToEndPoint == null || !farmHouse.isTileOnMap(__instance.controller.pathToEndPoint.Last().X, __instance.controller.pathToEndPoint.Last().Y))
                        __instance.controller = null;
                }
            }

            return true;
        }
    }
}