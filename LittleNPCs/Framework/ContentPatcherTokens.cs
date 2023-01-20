using System.Linq;
using System.Collections.Generic;


namespace LittleNPCs.Framework {
    internal static class ContentPatcherTokens {
        public static void Register(ModEntry modEntry) {
            var api = modEntry.Helper.ModRegistry.GetApi<ContentPatcher.IContentPatcherAPI>("Pathoschild.ContentPatcher");

            api.RegisterToken(modEntry.ModManifest, "FirstLittleNPCName", () => {
                string name = new LittleNPCInfo(0).Name;

                return name.ToTokenReturnValue();
            });

            api.RegisterToken(modEntry.ModManifest, "FirstLittleNPCGender", () => {
                string gender = new LittleNPCInfo(0).Gender;

                return gender.ToTokenReturnValue();
            });

            api.RegisterToken(modEntry.ModManifest, "SecondLittleNPCName", () => {
                string name = new LittleNPCInfo(1).Name;

                return name.ToTokenReturnValue();
            });

            api.RegisterToken(modEntry.ModManifest, "SecondLittleNPCGender", () => {
                string gender = new LittleNPCInfo(1).Gender;

                return gender.ToTokenReturnValue();
            });
        }

        private static IEnumerable<string> ToTokenReturnValue(this string value) {
            // Required format of CP return values if IEnumerable<string>.
            return string.IsNullOrEmpty(value) ? Enumerable.Empty<string>()
                                               : Enumerable.Repeat(value, 1);
        }
    }
}
