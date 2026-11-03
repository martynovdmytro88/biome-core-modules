using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;

namespace Biome.Utils.Commands
{
    /// <summary>The <c>invoke</c> door: (path, function, json params) → envelope.</summary>
    public static class InvokeCommand
    {
        public const string Name = "invoke";
        public const string Description =
            "Invoke a [BiomeCallable] static method found under a project path by name, mapping a JSON object of parameters onto its C# signature; the result is wrapped in the envelope.";

        /// <summary>
        /// Resolves a callable under <paramref name="path"/> (the asmdefs there name the assemblies) by method or
        /// <c>[CliCommand]</c> name, maps the JSON object in <paramref name="params"/> onto the C# parameters
        /// (by name; missing optional parameters take their defaults) and returns the method's result in the envelope
        /// payload. A thrown exception becomes <c>code: exception</c>. A result that is itself an envelope is passed
        /// through unchanged.
        /// </summary>
        /// <param name="path">Project-relative folder holding the asmdef(s), e.g. <c>Assets/&lt;modulesFolder&gt;/Foo</c>.</param>
        /// <param name="function">Method name or [CliCommand] name.</param>
        /// <param name="params">JSON object of named parameters, e.g. <c>{"name":"Foo"}</c>.</param>
        [BiomeCallable(CallableKind.Util)]
        [CliCommand(Name, Description, Tags = new[] { "biome/util" })]
        public static BiomeEnvelope Invoke(
            [CliArg("path", "Project-relative folder whose asmdefs scope the search", Required = true)] string path,
            [CliArg("function", "Method name or [CliCommand] name", Required = true)] string function,
            [CliArg("params", "JSON object of named parameters (default: {})")] string @params = null)
        {
            CallableInfo callable = null;
            try
            {
                callable = CallableResolver.Find(path, function, out var problem);
                if (callable == null) return BiomeEnvelope.Fail(Name, problem);

                JObject args;
                try { args = string.IsNullOrWhiteSpace(@params) ? new JObject() : JObject.Parse(@params); }
                catch (Exception ex) { return BiomeEnvelope.Fail(Name, "params is not a JSON object: " + ex.Message); }

                var values = BindArguments(callable.Method, args, out var bindProblem, out var warnings);
                if (bindProblem != null) return BiomeEnvelope.Fail(Name, bindProblem, warnings: warnings);

                var result = callable.Method.Invoke(null, values);
                if (result is BiomeEnvelope inner)
                {
                    inner.AddWarnings(warnings);
                    return inner;
                }
                return BiomeEnvelope.Ok(Name, $"{callable.TypeName}.{callable.Name} returned", result, warnings);
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                return BiomeEnvelope.Exception(callable?.CliName ?? function, tie.InnerException);
            }
            catch (Exception ex)
            {
                return BiomeEnvelope.Exception(Name, ex);
            }
        }

        /// <summary>Map a JSON object onto the method's parameters by name.</summary>
        public static object[] BindArguments(MethodInfo method, JObject args, out string problem, out List<string> warnings)
        {
            problem = null;
            warnings = new List<string>();
            var ps = method.GetParameters();
            var values = new object[ps.Length];
            var consumed = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < ps.Length; i++)
            {
                var p = ps[i];
                var cliName = p.GetCustomAttribute<CliArgAttribute>()?.Name;
                JToken token = null;
                var key = p.Name;
                if (args.TryGetValue(p.Name, out var byName)) token = byName;
                else if (cliName != null && args.TryGetValue(cliName, out var byCli)) { token = byCli; key = cliName; }

                if (token == null)
                {
                    if (p.HasDefaultValue) { values[i] = p.DefaultValue; continue; }
                    problem = $"missing required parameter '{cliName ?? p.Name}' ({CallableResolver.TypeName(p.ParameterType)})";
                    return values;
                }
                consumed.Add(key);
                try
                {
                    values[i] = token.Type == JTokenType.Null ? null : token.ToObject(p.ParameterType);
                }
                catch (Exception ex)
                {
                    problem = $"parameter '{key}' does not convert to {CallableResolver.TypeName(p.ParameterType)}: {ex.Message}";
                    return values;
                }
            }
            foreach (var prop in args.Properties())
                if (!consumed.Contains(prop.Name))
                    warnings.Add($"unknown parameter '{prop.Name}' ignored");
            return values;
        }
    }
}
