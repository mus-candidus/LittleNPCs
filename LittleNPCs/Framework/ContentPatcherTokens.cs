using System.Linq;
using System.Collections.Generic;


namespace LittleNPCs.Framework {
    internal static class ContentPatcherTokens {
        public static void Register(ModEntry modEntry) {
            var api = modEntry.Helper.ModRegistry.GetApi<ContentPatcher.IContentPatcherAPI>("Pathoschild.ContentPatcher");

            api.RegisterToken(modEntry.ModManifest, "FirstLittleNPCName", () => {
                string name = $"FirstLittleNPC{new LittleNPCInfo(0, modEntry.Monitor).Name}";

                modEntry.Monitor.Log($"FirstLittleNPCName() returns {name}");

                return name.ToTokenReturnValue();
            });

            api.RegisterToken(modEntry.ModManifest, "FirstLittleNPCGender", () => {
                string gender = new LittleNPCInfo(0, modEntry.Monitor).Gender;

                modEntry.Monitor.Log($"FirstLittleNPCGender() returns {gender}");

                return gender.ToTokenReturnValue();
            });

            api.RegisterToken(modEntry.ModManifest, "SecondLittleNPCName", () => {
                string name = $"SecondLittleNPC{new LittleNPCInfo(1, modEntry.Monitor).Name}";

                modEntry.Monitor.Log($"SecondLittleNPCName() returns {name}");

                return name.ToTokenReturnValue();
            });

            api.RegisterToken(modEntry.ModManifest, "SecondLittleNPCGender", () => {
                string gender = new LittleNPCInfo(1, modEntry.Monitor).Gender;

                modEntry.Monitor.Log($"SecondLittleNPCGender() returns {gender}");

                return gender.ToTokenReturnValue();
            });
        }

        private static IEnumerable<string> ToTokenReturnValue(this string value) {
            // Create an IEnumerable from value as required by CP.
            return string.IsNullOrEmpty(value) ? Enumerable.Empty<string>()
                                               : Enumerable.Repeat(value, 1);
        }
    }
}
