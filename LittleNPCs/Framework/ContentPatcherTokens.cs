
namespace LittleNPCs.Framework {
    internal static class ContentPatcherTokens {
        public static void Register(ModEntry modEntry) {
            var api = modEntry.Helper.ModRegistry.GetApi<ContentPatcher.IContentPatcherAPI>("Pathoschild.ContentPatcher");

            api.RegisterToken(modEntry.ModManifest, "FirstLittleNPCName", () => {
                string name = new LittleNPCInfo(0).Name;

                return (name is null) ? null : new string[] { name };
            });

            api.RegisterToken(modEntry.ModManifest, "FirstLittleNPCGender", () => {
                string gender = new LittleNPCInfo(0).Gender;

                return (gender is null) ? null : new string[] { gender };
            });

            api.RegisterToken(modEntry.ModManifest, "SecondLittleNPCName", () => {
                string name = new LittleNPCInfo(1).Name;

                return (name is null) ? null : new string[] { name };
            });

            api.RegisterToken(modEntry.ModManifest, "SecondLittleNPCGender", () => {
                string gender = new LittleNPCInfo(1).Gender;

                return (gender is null) ? null : new string[] { gender };
            });
        }
    }
}
