using System.Linq;
using System.Collections.Generic;

using StardewModdingAPI;


namespace LittleNPCs.Framework {
    internal static class ContentPatcherTokens {
        private class TokenImplementation {
            private LittleNPCInfo[] cachedLittleNPCs_ = new LittleNPCInfo[2];

            public TokenImplementation(ModEntry modEntry) {
                var api = modEntry.Helper.ModRegistry.GetApi<ContentPatcher.IContentPatcherAPI>("Pathoschild.ContentPatcher");

                api.RegisterToken(modEntry.ModManifest, "FirstLittleNPCName", () => {
                    UpdateFirstLittleNPC(modEntry.Monitor);

                    return cachedLittleNPCs_[0].Name.ToTokenReturnValue();
                });

                api.RegisterToken(modEntry.ModManifest, "FirstLittleNPCDisplayName", () => {
                    UpdateFirstLittleNPC(modEntry.Monitor);

                    return cachedLittleNPCs_[0].DisplayName.ToTokenReturnValue();
                });

                api.RegisterToken(modEntry.ModManifest, "FirstLittleNPCGender", () => {
                    UpdateFirstLittleNPC(modEntry.Monitor);

                    return cachedLittleNPCs_[0].Gender.ToTokenReturnValue();
                });

                api.RegisterToken(modEntry.ModManifest, "SecondLittleNPCName", () => {
                    UpdateSecondLittleNPC(modEntry.Monitor);

                    return cachedLittleNPCs_[1].Name.ToTokenReturnValue();
                });

                api.RegisterToken(modEntry.ModManifest, "SecondLittleNPCDisplayName", () => {
                    UpdateSecondLittleNPC(modEntry.Monitor);

                    return cachedLittleNPCs_[1].DisplayName.ToTokenReturnValue();
                });

                api.RegisterToken(modEntry.ModManifest, "SecondLittleNPCGender", () => {
                    UpdateSecondLittleNPC(modEntry.Monitor);

                    return cachedLittleNPCs_[1].Gender.ToTokenReturnValue();
                });
            }

            private void UpdateFirstLittleNPC(IMonitor monitor) {
                var littleNPC = new LittleNPCInfo(0, monitor);
                if (littleNPC is not null && !littleNPC.Equals(cachedLittleNPCs_[0])) {
                    cachedLittleNPCs_[0] = littleNPC;

                    monitor.Log($"FirstLittleNPC updated: {cachedLittleNPCs_[0]}");
                }
            }

            private void UpdateSecondLittleNPC(IMonitor monitor) {
                var littleNPC = new LittleNPCInfo(1, monitor);
                if (littleNPC is not null && !littleNPC.Equals(cachedLittleNPCs_[1])) {
                    cachedLittleNPCs_[1] = littleNPC;

                    monitor.Log($"SecondLittleNPC updated: {cachedLittleNPCs_[1]}");
                }
            }
        }

        public static void Register(ModEntry modEntry) {
            new TokenImplementation(modEntry);
        }

        private static IEnumerable<string> ToTokenReturnValue(this string value) {
            // Create an IEnumerable from value as required by CP.
            return string.IsNullOrEmpty(value) ? Enumerable.Empty<string>()
                                               : Enumerable.Repeat(value, 1);
        }
    }
}
