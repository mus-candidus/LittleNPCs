using Microsoft.Xna.Framework;
using StardewValley;
using StardewModdingAPI;
using System;

namespace ChildToNPC.Patches
{

    public class DefaultLocation
    {
        public string defaultMap { get; set; }
        public Vector2 defaultPosition { get; set; }

        public DefaultLocation(string defaultMap, Vector2 defaultPosition)
        {
            this.defaultMap = defaultMap;
            this.defaultPosition = defaultPosition;
        }
    }

    /// <summary>
    /// Class that holds patches against parseMasterSchedule
    /// </summary>
    /// <remarks>Assign the Child2NPC a fake start location to match the default start location of spouses.</remarks>
    class NPCParseMasterSchedulePatch
    {
        /// <summary>
        /// Handles `bed` before the vanilla function can, assigns Child2NPC a fake start location.
        /// </summary>
        /// <param name="__instance">NPC</param>
        /// <param name="rawData">Raw schedule string.</param>
        /// <param name="__state">Holds default start location, to restore later.</param>
        public static void Prefix(NPC __instance, ref string rawData, out DefaultLocation? __state)
        {
            if (!ModEntry.IsChildNPC(__instance))
            {
                __state = null;
                return;
            }
            
            // Scheduling code can use "bed" to refer to the usual last stop of an NPC
            // For a Child2NPC, this is always the bus stop, so I can just do the replacement here
            if (rawData.EndsWith("bed"))
            {
                rawData = rawData[..^3] + "BusStop -1 23 3";
            }

            // Save the previous default map and default position.
            __state = new DefaultLocation(
                defaultMap: __instance.DefaultMap,
                defaultPosition: __instance.DefaultPosition);

            // Pretending my start location is the bus stop location.
            __instance.DefaultMap = "BusStop";
            __instance.DefaultPosition = new Vector2(0,23)*64;

            return;
        }

        /// <summary>
        /// Restore default map and position.
        /// </summary>
        /// <param name="__instance">NPC</param>
        /// <param name="rawData">Raw schedule string.</param>
        /// <param name="__state">Default location for NPC, saved from prefix.</param>
        public static void Postfix(NPC __instance, string rawData, DefaultLocation? __state)
        {
            if (__state is not null)
            {
                __instance.DefaultMap = __state.defaultMap;
                __instance.DefaultPosition = __state.defaultPosition;
            }
        }

        /// <summary>
        /// Error handling - if parseMasterSchedule throws an error, refreshes the default location/
        /// </summary>
        /// <param name="__instance">NPC in question</param>
        /// <param name="__exception">The exception thrown.</param>
        public static void Finalizer(NPC __instance, Exception __exception)
        {
            if (__exception is not null)
            {
                __instance.reloadDefaultLocation();
            }
        }
    }
}
