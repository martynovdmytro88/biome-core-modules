using System;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace Biome.Configs.Editor.Tests
{
    /// <summary>Awaits an action that must fail and hands the exception back for the assertions.</summary>
    internal static class ConfigsTestAsync
    {
        public static async UniTask<TException> CatchAsync<TException>(Func<UniTask> action) where TException : Exception
        {
            try
            {
                await action();
            }
            catch (TException exception)
            {
                return exception;
            }

            Assert.Fail($"Expected {typeof(TException).Name}, but nothing was thrown.");
            return null;
        }
    }
}
