using System;
using System.Linq;
using System.Collections.Generic;

using StardewModdingAPI;
using StardewModdingAPI.Utilities;


namespace LittleNPCs.Framework {
    internal static class ContentPatcherTokens {
        /// <summary>
        /// Core implementation of a CP token that returns a single unbounded value.
        /// Implementation details are provided by function objects.
        /// </summary>
        private class TokenCore {
            /// <summary>Function called by <code>IsReady()</code>.</summary>
            private Func<bool> isReady_;

            /// <summary>Function called by <code>UpdateContext()</code>.</summary>
            private Func<bool> updateContext_;

            /// <summary>Function called by <code>GetValues()</code>.</summary>
            private Func<string, IEnumerable<string>> getValues_;

            /// <summary>Flag that determines whether input is required.</summary>
            private bool requiresInput_;

            public TokenCore(Func<bool> isReady, Func<bool> updateContext, Func<string, IEnumerable<string>> getValues, bool requiresInput) {
                isReady_       = isReady;
                updateContext_ = updateContext;
                getValues_     = getValues;
                requiresInput_ = requiresInput;
            }

            /// <summary>Get whether the values may change depending on the context.</summary>
            public bool IsMutable() => true;

            /// <summary>Get whether the token allows an input argument (e.g. an NPC name for a relationship token).</summary>
            public bool AllowsInput() => requiresInput_;

            /// <summary>Whether the token requires an input argument to work, and does not provide values without it (see <see cref="AllowsInput"/>).</summary>
            public bool RequiresInput() => requiresInput_;

            /// <summary>Whether the token may return multiple values for the given input.</summary>
            /// <param name="input">The input argument, if applicable.</param>
            public bool CanHaveMultipleValues(string input = null) => false;

            /// <summary>Get whether the token always chooses from a set of known values for the given input. Mutually exclusive with <see cref="HasBoundedRangeValues"/>.</summary>
            /// <param name="input">The input argument, if applicable.</param>
            /// <param name="allowedValues">The possible values for the input.</param>
            public bool HasBoundedValues(string input, out IEnumerable<string> allowedValues) {
                allowedValues = null;

                return false;
            }

            /// <summary>Update the values when the context changes.</summary>
            /// <returns>Returns whether the value changed, which may trigger patch updates.</returns>
            public bool UpdateContext() => updateContext_();


            /// <summary>Get whether the token is available for use.</summary>
            public bool IsReady() => isReady_();

            /// <summary>Get the current values.</summary>
            /// <param name="input">The input argument, if applicable.</param>
            public IEnumerable<string> GetValues(string input) => getValues_(input);
        }

        private class TokenImplementation {
            private LittleNPCInfo[] cachedLittleNPCs_ = new LittleNPCInfo[2];

            public TokenImplementation(ModEntry modEntry) {
                var api = modEntry.Helper.ModRegistry.GetApi<ContentPatcher.IContentPatcherAPI>("Pathoschild.ContentPatcher");

                List<(int childIndex, string tokenName, bool requiresInput, Func<LittleNPCInfo, string, IEnumerable<string>> tokenResultFunc)> tokens = new() {
                    // Old tokens, FirstLittleNPC.
                    (0, "FirstLittleNPCName", false, (npc, _) => TokenResult(npc, "Name")),
                    (0, "FirstLittleNPCDisplayName", false, (npc, _) => TokenResult(npc, "DisplayName")),
                    (0, "FirstLittleNPCGender", false, (npc, _) => TokenResult(npc, "Gender")),
                    (0, "FirstLittleNPCBirthSeason", false, (npc, _) => TokenResult(npc, "BirthSeason")),
                    (0, "FirstLittleNPCBirthDay", false, (npc, _) => TokenResult(npc, "BirthDay")),
                    (0, "FirstLittleNPCAge", false, (npc, _) => TokenResult(npc, "Age")),
                    // Old tokens, SecondLittleNPC.
                    (1, "SecondLittleNPCName", false, (npc, _) => TokenResult(npc, "Name")),
                    (1, "SecondLittleNPCDisplayName", false, (npc, _) => TokenResult(npc, "DisplayName")),
                    (1, "SecondLittleNPCGender", false, (npc, _) => TokenResult(npc, "Gender")),
                    (1, "SecondLittleNPCBirthSeason", false, (npc, _) => TokenResult(npc, "BirthSeason")),
                    (1, "SecondLittleNPCBirthDay", false, (npc, _) => TokenResult(npc, "BirthDay")),
                    (1, "SecondLittleNPCAge", false, (npc, _) => TokenResult(npc, "Age")),
                    // New tokens.
                    (0, "FirstLittleNPC", true, (npc, input) => TokenResult(npc, input)),
                    (1, "SecondLittleNPC", true, (npc, input) => TokenResult(npc, input))
                };

                foreach (var token in tokens) {
                    api.RegisterToken(modEntry.ModManifest, token.tokenName,
                        new TokenCore(
                            () => cachedLittleNPCs_[token.childIndex] is not null && cachedLittleNPCs_[token.childIndex].LoadedFrom != LittleNPCInfo.LoadState.None,
                            () => UpdateLittleNPC(token.childIndex),
                            (input) => token.tokenResultFunc(cachedLittleNPCs_[token.childIndex], input),
                            token.requiresInput
                        )
                    );
                }
            }

            private IEnumerable<string> TokenResult (LittleNPCInfo npc, string input) {
                yield return (input switch {
                    "Name"        => npc.Name,
                    "DisplayName" => npc.DisplayName,
                    "Gender"      => npc.Gender.ToString().ToLower(),
                    "BirthSeason" => npc.Birthday.Season.ToString(),
                    "BirthDay"    => npc.Birthday.Day.ToString(),
                    "Age"         => (SDate.Now().Year - npc.Birthday.Year).ToString(),
                    _             => string.Empty
                });
            }

            private bool UpdateLittleNPC(int childIndex) {
                var littleNPC = new LittleNPCInfo(childIndex);
                if (littleNPC.LoadedFrom != LittleNPCInfo.LoadState.None && !littleNPC.Equals(cachedLittleNPCs_[childIndex])) {
                    cachedLittleNPCs_[childIndex] = littleNPC;

                    string prefix = childIndex == 0 ? "FirstLittleNPC" : "SecondLittleNPC";
                    ModEntry.monitor_.Log($"[{LittleNPC.GetHostTag()}] {prefix} updated: {cachedLittleNPCs_[childIndex]}", LogLevel.Info);

                    return true;
                }

                return false;
            }
        }

        public static void Register(ModEntry modEntry) {
            new TokenImplementation(modEntry);
        }
    }
}
