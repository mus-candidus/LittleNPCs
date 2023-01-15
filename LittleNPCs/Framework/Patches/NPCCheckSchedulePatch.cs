using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using Netcode;

using LittleNPCs;
using LittleNPCs.Framework;


namespace LittleNPCs.Framework.Patches
{
    /* Prefix for checkSchedule
     * This is a mix of code from the original method and my own.
     * I use reflection to access private methods in the NPC class.
     */
    class NPCCheckSchedulePatch
    {
        public static bool Prefix(NPC __instance, int timeOfDay, ref Point ___previousEndPoint, ref string ___extraDialogueMessageToAddThisMorning, ref SchedulePathDescription ___directionsToNewLocation, ref Rectangle ___lastCrossroad, ref NetString ___endOfRouteBehaviorName, ref bool ___returningToEndPoint)
        {
            if (!(__instance is LittleNPC))
                return true;

            if (__instance.currentScheduleDelay == 0f && __instance.scheduleDelaySeconds > 0f)
            {
                __instance.currentScheduleDelay = __instance.scheduleDelaySeconds;
            }
            else
            {
                if (___returningToEndPoint)
                {
                    return false;
                }

                __instance.updatedDialogueYet = false;
                ___extraDialogueMessageToAddThisMorning = null;
                if (__instance.ignoreScheduleToday || __instance.Schedule == null)
                {
                    return false;
                }

                SchedulePathDescription value = null;
                if (__instance.lastAttemptedSchedule < timeOfDay)
                {
                    __instance.lastAttemptedSchedule = timeOfDay;
                    __instance.Schedule.TryGetValue(timeOfDay, out value);
                    if (value != null)
                    {
                        __instance.queuedSchedulePaths.Add(new KeyValuePair<int, SchedulePathDescription>(timeOfDay, value));
                    }
                    value = null;
                }

                //If I have curfew, override the normal behavior
                if (ModEntry.config_.DoChildrenHaveCurfew && !__instance.currentLocation.Equals(Game1.getLocationFromName("FarmHouse")))
                {
                    //Send child home for curfew
                    if(timeOfDay == ModEntry.config_.CurfewTime)
                    {
                        object[] pathfindParams = { __instance.currentLocation.Name, __instance.getTileX(), __instance.getTileY(), "BusStop", -1, 23, 3, null, null };
                        value = ModEntry.helper_
                                        .Reflection
                                        .GetMethod(__instance, "pathfindToNextScheduleLocation", true)
                                        .Invoke<SchedulePathDescription>(pathfindParams);
                        __instance.queuedSchedulePaths.Clear();
                        __instance.queuedSchedulePaths.Add(new KeyValuePair<int, SchedulePathDescription>(timeOfDay, value));
                    }
                    value = null;
                }

                if (__instance.controller != null && __instance.controller.pathToEndPoint != null && __instance.controller.pathToEndPoint.Count > 0)
                {
                    return false;
                }

                if (__instance.queuedSchedulePaths.Count > 0 && timeOfDay >= __instance.queuedSchedulePaths[0].Key)
                {
                    value = __instance.queuedSchedulePaths[0].Value;
                }

                if (value == null)
                {
                    return false;
                }

                //prepareToDisembarkOnNewSchedulePath();
                ModEntry.helper_
                        .Reflection
                        .GetMethod(__instance, "prepareToDisembarkOnNewSchedulePath", true)
                        .Invoke(null);

                if (___returningToEndPoint || __instance.temporaryController != null)
                {
                    return false;
                }

                __instance.DirectionsToNewLocation = value;
                if (__instance.queuedSchedulePaths.Count > 0)
                {
                    __instance.queuedSchedulePaths.RemoveAt(0);
                }

                __instance.controller = new PathFindController(__instance.DirectionsToNewLocation.route, __instance, Utility.getGameLocationOfCharacter(__instance))
                {
                    finalFacingDirection = __instance.DirectionsToNewLocation.facingDirection,
                    //endBehaviorFunction = __instance.getRouteEndBehaviorFunction(__instance.DirectionsToNewLocation.endOfRouteBehavior, __instance.DirectionsToNewLocation.endOfRouteMessage)
                    endBehaviorFunction = ModEntry.helper_
                                                  .Reflection
                                                  .GetMethod(__instance, "getRouteEndBehaviorFunction", true)
                                                  .Invoke<PathFindController.endBehavior>(__instance.DirectionsToNewLocation.endOfRouteBehavior, __instance.DirectionsToNewLocation.endOfRouteMessage)
                };

                if (__instance.controller.pathToEndPoint == null || __instance.controller.pathToEndPoint.Count == 0)
                {
                    if (__instance.controller.endBehaviorFunction != null)
                    {
                        __instance.controller.endBehaviorFunction(__instance, __instance.currentLocation);
                    }
                    __instance.controller = null;
                }

                if (__instance.DirectionsToNewLocation != null && __instance.DirectionsToNewLocation.route != null)
                {
                    ___previousEndPoint = ((__instance.DirectionsToNewLocation.route.Count > 0) ? __instance.DirectionsToNewLocation.route.Last() : Point.Zero);
                }
            }

            return false;
        }
    }
}
