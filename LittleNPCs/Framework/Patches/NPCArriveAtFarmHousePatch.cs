using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Network;

using LittleNPCs;
using LittleNPCs.Framework;


namespace LittleNPCs.Framework.Patches
{
    /* Postfix for arriveAtFarmHouse
     * This code is directly translated from the original method
     * because the original method would immediately kick out non-married NPCs.
     */
    class NPCArriveAtFarmHousePatch
    {
        public static void Postfix(NPC __instance, FarmHouse farmHouse)
        {
            if (!(__instance is LittleNPC))
                return;

            __instance.setTilePosition(farmHouse.getEntryLocation());
            __instance.ignoreScheduleToday = true;
            __instance.temporaryController = null;
            __instance.controller = null;
            
            if(ModEntry.config_.DoChildrenHaveCurfew && Game1.timeOfDay >= ModEntry.config_.CurfewTime)
            {
                Point bedPoint = new Point((int)__instance.DefaultPosition.X / 64, (int)__instance.DefaultPosition.Y / 64);
                __instance.controller = new PathFindController(__instance, farmHouse, bedPoint, 2);
                //__instance.controller = new PathFindController(__instance, farmHouse, bedPoint, 0, new PathFindController.endBehavior(FarmHouse.spouseSleepEndFunction));
            }
            else
            {
                __instance.controller = new PathFindController(__instance, farmHouse, farmHouse.getRandomOpenPointInHouse(Game1.random, 0, 30), 2);
            }

            if(__instance.controller.pathToEndPoint == null)
            {
                __instance.willDestroyObjectsUnderfoot = true;
                __instance.controller = new PathFindController(__instance, farmHouse, farmHouse.getRandomOpenPointInHouse(Game1.random, 0, 30), 0);
                //__instance.setNewDialogue(Game1.LoadStringByGender(__instance.Gender, "Strings/StringsFromCSFiles:NPC.cs.4500"), false, false);
            }

            if (Game1.currentLocation == farmHouse)
                Game1.currentLocation.playSound("doorClose", NetAudio.SoundContext.NPC);
        }
    }
}