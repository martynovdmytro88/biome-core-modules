using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Biome.Utils.Commands
{
    /// <summary>One scanned callable: reflection facts + the <c>///</c> block read from its source file.</summary>
    public sealed class ScannedCallable
    {
        public string name;            // method name
        public string cliName;         // [CliCommand] name or null
        public string kind;
        public string assembly;
        public string type;
        public string signature;
        public string sourceFile;      // project-relative or null when not located
        public string summary;         // from ///, "" when missing
        public Dictionary<string, string> paramDocs = new Dictionary<string, string>();
        public string returns;
        public bool hasDoc;
        public string hash;            // sha256(signature + doc) first 12 hex

        /// <summary>The name the README and the catalogue key on: the CLI name when exposed, else the method name.</summary>
        public string Key => cliName ?? name;
    }

    /// <summary>
    /// Scans the callables under a path: reflection (via <see cref="CallableResolver"/>) for the facts, a small
    /// forgiving parser for the <c>///</c> block above each attributed method. Missing doc = <c>no-doc</c>.
    /// </summary>
    public static class CallableScanner
    {
        private static readonly Regex DocLine = new Regex(@"^\s*///\s?(.*)$", RegexOptions.Compiled);
        private static readonly Regex AttrLine = new Regex(@"^\s*\[.*\]\s*$", RegexOptions.Compiled);

        public static List<ScannedCallable> Scan(string projectRelativePath, out List<string> warnings)
        {
            warnings = new List<string>();
            var sources = LoadSources(projectRelativePath);
            var result = new List<ScannedCallable>();
            foreach (var c in CallableResolver.CallablesUnder(projectRelativePath))
            {
                var sc = new ScannedCallable
                {
                    name = c.Name,
                    cliName = c.CliName,
                    kind = c.Kind.ToString().ToLowerInvariant(),
                    assembly = c.AssemblyName,
                    type = c.TypeName,
                    signature = CallableResolver.Signature(c.Method),
                };
                var raw = FindDoc(sources, c.Name, out sc.sourceFile);
                sc.hasDoc = !string.IsNullOrWhiteSpace(raw);
                if (sc.hasDoc) ParseDoc(raw, sc);
                else warnings.Add($"no-doc: {sc.Key} ({sc.type}.{sc.name}) has no /// block");
                sc.hash = Hash(sc.signature + "\n" + (raw ?? ""));
                result.Add(sc);
            }
            return result;
        }

        public static string Hash(string text)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text.Replace("\r\n", "\n")));
            var sb = new StringBuilder();
            for (var i = 0; i < 6; i++) sb.Append(bytes[i].ToString("x2"));
            return sb.ToString();
        }

        private static Dictionary<string, string[]> LoadSources(string projectRelativePath)
        {
            var abs = BiomeUtilsPaths.Absolute(projectRelativePath);
            var map = new Dictionary<string, string[]>();
            if (!Directory.Exists(abs)) return map;
            foreach (var f in Directory.EnumerateFiles(abs, "*.cs", SearchOption.AllDirectories))
                map[f] = File.ReadAllLines(f);
            return map;
        }

        /// <summary>The raw <c>///</c> text (tags kept) above the [BiomeCallable]-attributed declaration of the method.</summary>
        private static string FindDoc(Dictionary<string, string[]> sources, string method, out string sourceFile)
        {
            sourceFile = null;
            var decl = new Regex(@"\bstatic\b[^=;(]*\b" + Regex.Escape(method) + @"\s*\(", RegexOptions.Compiled);
            foreach (var kv in sources)
            {
                var lines = kv.Value;
                for (var i = 0; i < lines.Length; i++)
                {
                    if (!decl.IsMatch(lines[i])) continue;
                    // walk up over attribute lines; require [BiomeCallable among them
                    var j = i - 1;
                    var sawBiome = false;
                    while (j >= 0 && AttrLine.IsMatch(lines[j]))
                    {
                        if (lines[j].Contains("[BiomeCallable")) sawBiome = true;
                        j--;
                    }
                    if (!sawBiome) continue;
                    sourceFile = BiomeUtilsPaths.ToProjectRelative(kv.Key);
                    var doc = new List<string>();
                    while (j >= 0 && DocLine.IsMatch(lines[j]))
                    {
                        doc.Add(DocLine.Match(lines[j]).Groups[1].Value);
                        j--;
                    }
                    doc.Reverse();
                    return doc.Count == 0 ? null : string.Join("\n", doc);
                }
            }
            return null;
        }

        private static readonly Regex Summary = new Regex(@"<summary>(.*?)</summary>", RegexOptions.Singleline);
        private static readonly Regex Param = new Regex(@"<param\s+name\s*=\s*""([^""]+)""\s*>(.*?)</param>", RegexOptions.Singleline);
        private static readonly Regex Returns = new Regex(@"<returns>(.*?)</returns>", RegexOptions.Singleline);
        private static readonly Regex Tag = new Regex(@"<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex ParamRef = new Regex(@"<paramref\s+name\s*=\s*""([^""]+)""\s*/>", RegexOptions.Compiled);
        private static readonly Regex SeeRef = new Regex(@"<see\s+cref\s*=\s*""([^""]+)""\s*/>", RegexOptions.Compiled);

        private static void ParseDoc(string raw, ScannedCallable sc)
        {
            var s = Summary.Match(raw);
            sc.summary = Clean(s.Success ? s.Groups[1].Value : raw);
            foreach (Match m in Param.Matches(raw)) sc.paramDocs[m.Groups[1].Value] = Clean(m.Groups[2].Value);
            var r = Returns.Match(raw);
            sc.returns = r.Success ? Clean(r.Groups[1].Value) : null;
        }

        private static string Clean(string s)
        {
            s = s.Replace("<c>", "`").Replace("</c>", "`");
            s = ParamRef.Replace(s, "`$1`");
            s = SeeRef.Replace(s, "`$1`");
            s = Tag.Replace(s, "");
            s = s.Replace("&lt;", "<").Replace("&gt;", ">");
            return Regex.Replace(s, @"\s+", " ").Trim();
        }
    }
}
