using System.Linq;
using System.Collections.Generic;


namespace LittleNPCs.Framework {
    internal static class ContentPatcherTokens {
        private class TokenImplementation {
            private LittleNPCInfo[] cachedLittleNPCs_ = new LittleNPCInfo[2];

            public TokenImplementation(ModEntry modEntry) {
                var api = modEntry.Helper.ModRegistry.GetApi<ContentPatcher.IContentPatcherAPI>("Pathoschild.ContentPatcher");

                api.RegisterToken(modEntry.ModManifest, "FirstLittleNPCName", () => {
                    var littleNPC = new LittleNPCInfo(0, modEntry.Monitor);
                    if (littleNPC is not null && !littleNPC.Equals(cachedLittleNPCs_[0])) {
                        cachedLittleNPCs_[0] = littleNPC;

                        modEntry.Monitor.Log($"FirstLittleNPCName() returns {cachedLittleNPCs_[0].Name}");

                        return cachedLittleNPCs_[0].Name.ToTokenReturnValue();
                    }

                    return cachedLittleNPCs_[0].Name.ToTokenReturnValue();
                });

                api.RegisterToken(modEntry.ModManifest, "FirstLittleNPCDisplayName", () => {
                    var littleNPC = new LittleNPCInfo(0, modEntry.Monitor);
                    if (littleNPC is not null && !littleNPC.Equals(cachedLittleNPCs_[0])) {
                        cachedLittleNPCs_[0] = littleNPC;

                        modEntry.Monitor.Log($"FirstLittleNPCDisplayName() returns {cachedLittleNPCs_[0].DisplayName}");

                        return cachedLittleNPCs_[0].DisplayName.ToTokenReturnValue();
                    }

                    return cachedLittleNPCs_[0].DisplayName.ToTokenReturnValue();
                });

                api.RegisterToken(modEntry.ModManifest, "FirstLittleNPCGender", () => {
                    var littleNPC = new LittleNPCInfo(0, modEntry.Monitor);
                    if (littleNPC is not null && !littleNPC.Equals(cachedLittleNPCs_[0])) {
                        cachedLittleNPCs_[0] = littleNPC;

                        modEntry.Monitor.Log($"FirstLittleNPCGender() returns {cachedLittleNPCs_[0].Gender}");

                        return cachedLittleNPCs_[0].Gender.ToTokenReturnValue();
                    }

                    return cachedLittleNPCs_[0].Gender.ToTokenReturnValue();
                });

                api.RegisterToken(modEntry.ModManifest, "SecondLittleNPCName", () => {
                    var littleNPC = new LittleNPCInfo(1, modEntry.Monitor);
                    if (littleNPC is not null && !littleNPC.Equals(cachedLittleNPCs_[1])) {
                        cachedLittleNPCs_[1] = littleNPC;

                        modEntry.Monitor.Log($"SecondLittleNPCName() returns {cachedLittleNPCs_[1].Name}");

                        return cachedLittleNPCs_[1].Name.ToTokenReturnValue();
                    }

                    return cachedLittleNPCs_[1].Name.ToTokenReturnValue();
                });

                api.RegisterToken(modEntry.ModManifest, "SecondLittleNPCDisplayName", () => {
                    var littleNPC = new LittleNPCInfo(1, modEntry.Monitor);
                    if (littleNPC is not null && !littleNPC.Equals(cachedLittleNPCs_[1])) {
                        cachedLittleNPCs_[1] = littleNPC;

                        modEntry.Monitor.Log($"SecondLittleNPCDisplayName() returns {cachedLittleNPCs_[1].DisplayName}");

                        return cachedLittleNPCs_[1].DisplayName.ToTokenReturnValue();
                    }

                    return cachedLittleNPCs_[1].DisplayName.ToTokenReturnValue();
                });

                api.RegisterToken(modEntry.ModManifest, "SecondLittleNPCGender", () => {
                    var littleNPC = new LittleNPCInfo(1, modEntry.Monitor);
                    if (littleNPC is not null && !littleNPC.Equals(cachedLittleNPCs_[1])) {
                        cachedLittleNPCs_[1] = littleNPC;

                        modEntry.Monitor.Log($"SecondLittleNPCGender() returns {cachedLittleNPCs_[1].Gender}");

                        return cachedLittleNPCs_[1].Gender.ToTokenReturnValue();
                    }

                    return cachedLittleNPCs_[1].Gender.ToTokenReturnValue();
                });
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
