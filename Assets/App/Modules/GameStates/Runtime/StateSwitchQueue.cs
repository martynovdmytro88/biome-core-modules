using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Biome.GameStates
{
    /// <summary>Runs switch jobs one at a time in the order they were requested.</summary>
    internal sealed class StateSwitchQueue
    {
        private readonly Queue<Entry> _pending = new Queue<Entry>();
        private bool _draining;

        public UniTask EnqueueAsync(Func<CancellationToken, UniTask> job, CancellationToken ct)
        {
            var completion = new UniTaskCompletionSource();
            _pending.Enqueue(new Entry(job, ct, completion));
            if (!_draining)
            {
                DrainAsync().Forget();
            }

            return completion.Task;
        }

        private async UniTaskVoid DrainAsync()
        {
            _draining = true;
            try
            {
                while (_pending.Count > 0)
                {
                    var entry = _pending.Dequeue();
                    if (entry.Token.IsCancellationRequested)
                    {
                        entry.Completion.TrySetCanceled(entry.Token);
                        continue;
                    }

                    try
                    {
                        await entry.Job(entry.Token);
                        entry.Completion.TrySetResult();
                    }
                    catch (OperationCanceledException)
                    {
                        entry.Completion.TrySetCanceled(entry.Token);
                    }
                    catch (Exception exception)
                    {
                        entry.Completion.TrySetException(exception);
                    }
                }
            }
            finally
            {
                _draining = false;
            }
        }

        private readonly struct Entry
        {
            public readonly Func<CancellationToken, UniTask> Job;
            public readonly CancellationToken Token;
            public readonly UniTaskCompletionSource Completion;

            public Entry(Func<CancellationToken, UniTask> job, CancellationToken token, UniTaskCompletionSource completion)
            {
                Job = job;
                Token = token;
                Completion = completion;
            }
        }
    }
}
