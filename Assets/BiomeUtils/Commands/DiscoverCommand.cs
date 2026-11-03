using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Pipeline.Commands;
using UnityEditor;

namespace Biome.Utils.Commands
{
    /// <summary>The <c>discover</c> door: README emitter (<c>refresh</c>) and drift reporter (<c>check</c>).</summary>
    public static class DiscoverCommand
    {
        public const string Name = "discover";
        public const string Description =
            "Scan the [BiomeCallable] methods under a path. mode=refresh rewrites only the generated README section between the markers (byte-diff first); mode=check writes nothing and reports drift, no-doc and config mismatch.";
        public const string MarkerStart = "<!-- biome:generated:start -->";
        public const string MarkerEnd = "<!-- biome:generated:end -->";
        public const string DefaultIdentityPath = "../CONFIGS/project-identity.md";

        private static readonly Regex HashLine = new Regex(@"<!--\s*h:\s*(\S+)\s+([0-9a-f]+)\s*-->", RegexOptions.Compiled);

        /// <summary>
        /// Scans the callables under <paramref name="path"/> (attribute + <c>///</c>). With <c>mode=refresh</c> the
        /// generated section of <c>&lt;path&gt;/README.md</c> (between the markers) is rewritten — byte-diff first, so an
        /// unchanged README is not touched; the human text outside the markers is never modified. With
        /// <c>mode=check</c> nothing is written: per-callable hash (signature + doc) is compared with the README,
        /// <c>no-doc</c> callables are listed, and the tool config is compared with the vault's project identity.
        /// Codes: clean → <c>ok</c>; warnings only → <c>ok</c> + <c>warnings[]</c>; drift → <c>fail</c>.
        /// </summary>
        /// <param name="path">Project-relative folder to scan (its asmdefs scope the callables).</param>
        /// <param name="mode"><c>refresh</c> | <c>check</c>.</param>
        /// <param name="identity_path">check only: the vault identity file, relative to the Unity project root.</param>
        [BiomeCallable(CallableKind.Util)]
        [CliCommand(Name, Description, Tags = new[] { "biome/util" })]
        public static BiomeEnvelope Discover(
            [CliArg("path", "Project-relative folder to scan", Required = true)] string path,
            [CliArg("mode", "refresh | check")] string mode = "check",
            [CliArg("identity_path", "check: vault identity file relative to the project root")] string identity_path = DefaultIdentityPath)
        {
            try
            {
                mode = (mode ?? "check").Trim().ToLowerInvariant();
                if (mode != "refresh" && mode != "check")
                    return BiomeEnvelope.Fail(Name, $"mode must be refresh | check (got '{mode}')");
                if (!Directory.Exists(BiomeUtilsPaths.Absolute(path)))
                    return BiomeEnvelope.Fail(Name, $"path not found: {path}");

                var callables = CallableScanner.Scan(path, out var warnings);
                var readmeRel = path.TrimEnd('/') + "/README.md";
                var readmeAbs = BiomeUtilsPaths.Absolute(readmeRel);

                return mode == "refresh"
                    ? Refresh(path, callables, warnings, readmeRel, readmeAbs)
                    : Check(path, callables, warnings, readmeRel, readmeAbs, identity_path);
            }
            catch (Exception ex)
            {
                return BiomeEnvelope.Exception(Name, ex);
            }
        }

        // ── refresh ────────────────────────────────────────────────────────────────────────────────
        private static BiomeEnvelope Refresh(string path, List<ScannedCallable> callables, List<string> warnings,
            string readmeRel, string readmeAbs)
        {
            if (!File.Exists(readmeAbs))
                return BiomeEnvelope.Fail(Name, $"README not found: {readmeRel} (lay it with the marker frame first)");
            var before = File.ReadAllText(readmeAbs);
            if (!TrySplit(before, out var head, out _, out var tail))
                return BiomeEnvelope.Fail(Name, $"{readmeRel} has no generated markers ({MarkerStart} … {MarkerEnd})");

            var after = head + MarkerStart + "\n" + Render(path, callables) + MarkerEnd + tail;
            var changed = !string.Equals(before, after, StringComparison.Ordinal);
            if (changed)
            {
                File.WriteAllText(readmeAbs, after);
                AssetDatabase.ImportAsset(readmeRel);
            }
            var payload = new
            {
                readme = readmeRel,
                changed,
                callables = callables.Select(c => new { key = c.Key, c.kind, c.hash, c.hasDoc }).ToList(),
            };
            return BiomeEnvelope.Ok(Name, changed ? $"{readmeRel} generated section rewritten ({callables.Count} callables)"
                                                  : $"{readmeRel} already up to date ({callables.Count} callables)", payload, warnings);
        }

        /// <summary>The generated section body (between the markers, exclusive).</summary>
        public static string Render(string path, List<ScannedCallable> callables)
        {
            var sb = new StringBuilder();
            sb.Append("<!-- emitted by discover --mode refresh; do not edit by hand -->\n");
            sb.Append($"## Callables under `{path}` ({callables.Count})\n\n");
            if (callables.Count == 0) sb.Append("_none_\n\n");
            foreach (var c in callables)
            {
                sb.Append($"### `{c.Key}` — {c.kind}\n");
                sb.Append($"<!-- h: {c.Key} {c.hash} -->\n");
                sb.Append($"- **method:** `{c.type}.{c.name}` · **asmdef:** `{c.assembly}`");
                if (c.cliName != null) sb.Append($" · **cli:** `unity command {c.cliName}`");
                sb.Append('\n');
                sb.Append($"- **signature:** `{c.signature}`\n");
                sb.Append(c.hasDoc ? $"- **doc:** {c.summary}\n" : "- **doc:** _no-doc_ ⚠\n");
                foreach (var p in c.paramDocs) sb.Append($"  - `{p.Key}` — {p.Value}\n");
                if (!string.IsNullOrEmpty(c.returns)) sb.Append($"- **returns:** {c.returns}\n");
                sb.Append('\n');
            }
            return sb.ToString();
        }

        public static bool TrySplit(string text, out string head, out string body, out string tail)
        {
            head = body = tail = null;
            var s = text.IndexOf(MarkerStart, StringComparison.Ordinal);
            if (s < 0) return false;
            var e = text.IndexOf(MarkerEnd, s + MarkerStart.Length, StringComparison.Ordinal);
            if (e < 0) return false;
            head = text.Substring(0, s);
            body = text.Substring(s + MarkerStart.Length, e - (s + MarkerStart.Length));
            tail = text.Substring(e + MarkerEnd.Length);
            return true;
        }

        // ── check ──────────────────────────────────────────────────────────────────────────────────
        private static BiomeEnvelope Check(string path, List<ScannedCallable> callables, List<string> warnings,
            string readmeRel, string readmeAbs, string identityPath)
        {
            var drift = new List<string>();

            // README hashes
            var recorded = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!File.Exists(readmeAbs)) drift.Add($"readme-missing: {readmeRel}");
            else if (!TrySplit(File.ReadAllText(readmeAbs), out _, out var body, out _)) drift.Add($"readme-no-markers: {readmeRel}");
            else foreach (Match m in HashLine.Matches(body)) recorded[m.Groups[1].Value] = m.Groups[2].Value;

            if (File.Exists(readmeAbs))
            {
                foreach (var c in callables)
                {
                    if (!recorded.TryGetValue(c.Key, out var h)) drift.Add($"not-in-readme: {c.Key}");
                    else if (h != c.hash) drift.Add($"drift: {c.Key} (readme {h} ≠ live {c.hash})");
                }
                foreach (var k in recorded.Keys.Where(k => callables.All(c => c.Key != k)))
                    drift.Add($"stale-in-readme: {k}");
            }

            // config ↔ project identity
            var configDrift = CheckConfig(identityPath, out var configWarnings);
            drift.AddRange(configDrift);
            warnings.AddRange(configWarnings);

            var payload = new
            {
                path,
                readme = readmeRel,
                callables = callables.Select(c => new { key = c.Key, c.kind, c.hash, c.hasDoc }).ToList(),
                drift,
                warnings,
            };
            if (drift.Count > 0)
                return BiomeEnvelope.Fail(Name, $"drift ({drift.Count}): {string.Join("; ", drift)}", payload, warnings);
            return BiomeEnvelope.Ok(Name, warnings.Count == 0 ? $"clean ({callables.Count} callables)"
                                                              : $"clean with {warnings.Count} warning(s) ({callables.Count} callables)", payload, warnings);
        }

        /// <summary>biome-utils.json ↔ CONFIGS/project-identity.md (project-name → modules folder, asmdef-root).</summary>
        public static List<string> CheckConfig(string identityPath, out List<string> warnings)
        {
            warnings = new List<string>();
            var drift = new List<string>();
            BiomeUtilsConfig cfg;
            try { cfg = BiomeUtilsConfig.Current; }
            catch (Exception ex) { drift.Add("config-unreadable: " + ex.Message); return drift; }

            var abs = BiomeUtilsPaths.Absolute(identityPath ?? DefaultIdentityPath);
            if (!File.Exists(abs))
            {
                warnings.Add($"identity-unreachable: {identityPath} not found — config ↔ identity not verified");
                return drift;
            }
            var identity = ParseIdentity(File.ReadAllText(abs));
            if (identity.TryGetValue("project-name", out var project))
            {
                var expected = project + "/Modules";
                if (!string.Equals(expected, cfg.ModulesFolder, StringComparison.Ordinal))
                    drift.Add($"config: modulesFolder '{cfg.ModulesFolder}' ≠ identity '{expected}'");
            }
            else warnings.Add("identity: project-name missing");
            if (identity.TryGetValue("asmdef-root", out var root))
            {
                if (!string.Equals(root, cfg.AsmdefRoot, StringComparison.Ordinal))
                    drift.Add($"config: asmdefRoot '{cfg.AsmdefRoot}' ≠ identity '{root}'");
            }
            else warnings.Add("identity: asmdef-root missing");
            return drift;
        }

        private static readonly Regex IdentityKey = new Regex(@"^\s*([a-z-]+):\s*([^#\s]+)", RegexOptions.Multiline);

        public static Dictionary<string, string> ParseIdentity(string text)
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match m in IdentityKey.Matches(text)) d[m.Groups[1].Value] = m.Groups[2].Value.Trim();
            return d;
        }

        // ── hands ──────────────────────────────────────────────────────────────────────────────────
        [MenuItem("Biome/Utils/Discover — refresh BiomeUtils README", priority = 20)]
        private static void RefreshMenu() => Log(Discover(BiomeUtilsPaths.Root, "refresh"));

        [MenuItem("Biome/Utils/Discover — check BiomeUtils", priority = 21)]
        private static void CheckMenu() => Log(Discover(BiomeUtilsPaths.Root, "check"));

        private static void Log(BiomeEnvelope env)
        {
            var text = $"[BiomeUtils] discover {env.Code}: {env.Message}";
            if (env.Code == ResultCode.Ok) UnityEngine.Debug.Log(text); else UnityEngine.Debug.LogWarning(text);
        }
    }
}
