using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.Pipeline.Models;

namespace Biome.Utils.Commands
{
    /// <summary>Result codes of the envelope. String constants so the JSON is readable without a lookup.</summary>
    public static class ResultCode
    {
        public const string Ok = "ok";
        public const string Fail = "fail";
        public const string Exception = "exception";
    }

    /// <summary>
    /// The envelope every callable returns: <c>{ code, message, channel, warnings[], payload }</c>.
    /// Derives from the pipeline's <see cref="BaseResponse"/> so a <c>fail</c> / <c>exception</c> also sets
    /// <c>error</c>, which the pipeline server reads as a failed command (non-zero CLI exit).
    /// </summary>
    public sealed class BiomeEnvelope : BaseResponse
    {
        [JsonProperty("code")]
        public string Code { get; set; } = ResultCode.Ok;

        /// <summary>The callable that produced the result, e.g. <c>create_module</c>.</summary>
        [JsonProperty("channel")]
        public string Channel { get; set; }

        [JsonProperty("payload")]
        public object Payload { get; set; }

        public static BiomeEnvelope Ok(string channel, string message, object payload, IEnumerable<string> warnings = null)
        {
            var env = new BiomeEnvelope { Code = ResultCode.Ok, Channel = channel, Message = message, Payload = payload };
            env.AddWarnings(warnings);
            return env;
        }

        public static BiomeEnvelope Fail(string channel, string message, object payload = null, IEnumerable<string> warnings = null)
        {
            var env = new BiomeEnvelope { Code = ResultCode.Fail, Channel = channel, Message = message, Payload = payload, Error = message };
            env.AddWarnings(warnings);
            return env;
        }

        public static BiomeEnvelope Exception(string channel, System.Exception ex, object payload = null)
        {
            return new BiomeEnvelope
            {
                Code = ResultCode.Exception,
                Channel = channel,
                Message = ex.GetType().Name + ": " + ex.Message,
                Payload = payload,
                Error = ex.Message,
                ErrorDetails = ex.ToString(),
            };
        }

        public void AddWarnings(IEnumerable<string> warnings)
        {
            if (warnings == null) return;
            Warnings ??= new List<string>();
            Warnings.AddRange(warnings);
        }
    }
}
