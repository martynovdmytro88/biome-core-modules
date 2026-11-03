using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace Biome.Utils.Commands
{
    /// <summary>A callable found under a path: the method + its kind + the asmdef that hosts it.</summary>
    public sealed class CallableInfo
    {
        public MethodInfo Method;
        public CallableKind Kind;
        public string AssemblyName;
        public string CliName;          // [CliCommand] name, if present
        public string CliDescription;   // [CliCommand] description, if present
        public string Name => Method.Name;
        public string TypeName => Method.DeclaringType?.FullName;
    }

    /// <summary>
    /// Resolves <c>[BiomeCallable]</c> methods by a project path: the asmdefs under the path name the assemblies,
    /// TypeCache lists the attributed methods, the intersection is the callable set of that path.
    /// </summary>
    public static class CallableResolver
    {
        /// <summary>Assembly names of every asmdef under <paramref name="projectRelativePath"/> (recursive).</summary>
        public static List<string> AssembliesUnder(string projectRelativePath)
        {
            var abs = BiomeUtilsPaths.Absolute(projectRelativePath);
            if (!Directory.Exists(abs)) return new List<string>();
            var names = new List<string>();
            foreach (var asmdef in Directory.EnumerateFiles(abs, "*.asmdef", SearchOption.AllDirectories))
            {
                string name = null;
                try { name = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(asmdef))["name"]?.ToString(); }
                catch (Exception) { /* malformed asmdef → fall back to the file name */ }
                names.Add(string.IsNullOrEmpty(name) ? Path.GetFileNameWithoutExtension(asmdef) : name);
            }
            return names;
        }

        /// <summary>Every callable in the assemblies under the path, ordered by type then method name.</summary>
        public static List<CallableInfo> CallablesUnder(string projectRelativePath)
        {
            var assemblies = new HashSet<string>(AssembliesUnder(projectRelativePath), StringComparer.Ordinal);
            var found = new List<CallableInfo>();
            foreach (var method in TypeCache.GetMethodsWithAttribute<BiomeCallableAttribute>())
            {
                var asm = method.DeclaringType?.Assembly.GetName().Name;
                if (asm == null || !assemblies.Contains(asm)) continue;
                if (!method.IsStatic) continue;
                var attr = method.GetCustomAttribute<BiomeCallableAttribute>();
                var cli = method.GetCustomAttribute<Unity.Pipeline.Commands.CliCommandAttribute>();
                found.Add(new CallableInfo
                {
                    Method = method,
                    Kind = attr.Kind,
                    AssemblyName = asm,
                    CliName = cli?.Name,
                    CliDescription = cli?.Description,
                });
            }
            return found.OrderBy(c => c.TypeName, StringComparer.Ordinal).ThenBy(c => c.Name, StringComparer.Ordinal).ToList();
        }

        /// <summary>Find one callable by method name or [CliCommand] name under the path.</summary>
        public static CallableInfo Find(string projectRelativePath, string function, out string problem)
        {
            problem = null;
            var all = CallablesUnder(projectRelativePath);
            var hits = all.Where(c => string.Equals(c.Name, function, StringComparison.Ordinal)
                                   || string.Equals(c.CliName, function, StringComparison.Ordinal)).ToList();
            if (hits.Count == 1) return hits[0];
            if (hits.Count == 0)
            {
                problem = all.Count == 0
                    ? $"no [BiomeCallable] under '{projectRelativePath}' (no asmdef there, or nothing attributed)"
                    : $"no callable '{function}' under '{projectRelativePath}'; known: {string.Join(", ", all.Select(c => c.Name))}";
                return null;
            }
            problem = $"'{function}' is ambiguous under '{projectRelativePath}': {string.Join(", ", hits.Select(c => c.TypeName + "." + c.Name))}";
            return null;
        }

        /// <summary>A stable, human-readable signature: <c>ReturnType Type.Method(ParamType name, …)</c>.</summary>
        public static string Signature(MethodInfo m)
        {
            var ps = m.GetParameters().Select(p => $"{TypeName(p.ParameterType)} {p.Name}{(p.HasDefaultValue ? " = " + (p.DefaultValue ?? "null") : "")}");
            return $"{TypeName(m.ReturnType)} {m.DeclaringType?.FullName}.{m.Name}({string.Join(", ", ps)})";
        }

        public static string TypeName(Type t)
        {
            if (t == typeof(void)) return "void";
            if (t.IsArray) return TypeName(t.GetElementType()) + "[]";
            if (t.IsGenericType)
            {
                var name = t.Name.Substring(0, t.Name.IndexOf('`'));
                return $"{name}<{string.Join(", ", t.GetGenericArguments().Select(TypeName))}>";
            }
            return t.Name;
        }
    }
}
