using System;
using System.Collections.Generic;
using System.Threading;
using Biome.EventBus.Abstract;
using Biome.EventBus.Config;
using Biome.EventBus.Data;

namespace Biome.EventBus
{
    /// <summary>
    /// The production bus: synchronous, no package. Each bus takes a process-unique index; the typed channel of
    /// <c>TData</c> lives in <c>ChannelSlot&lt;TData&gt;</c> at that index, so the hot path is an array read with no
    /// Type lookup and no cast. The index is recycled on <see cref="Dispose"/> after every channel has cleared its slot.
    /// </summary>
    internal sealed class EventBus : IEventBus
    {
        private static readonly object s_IndexLock = new object();
        private static readonly Stack<int> s_FreeIndices = new Stack<int>();
        private static int s_NextIndex;

        private readonly int _index;
        private readonly int _ownerThreadId;
        private readonly List<ChannelBase> _channels = new List<ChannelBase>();
        private int _depth;
        private bool _disposed;

        internal readonly bool Isolate;
        internal readonly int MaxDispatchDepth;
        internal readonly int InitialListenerCapacity;
        internal readonly IEventErrorSink ErrorSink;
        internal readonly IEventTrace Trace; // null unless tracing is enabled

        public EventBus(EventBusConfig config, IEventErrorSink errorSink = null, IEventTrace trace = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (config.MaxDispatchDepth < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(config), config.MaxDispatchDepth, "MaxDispatchDepth must be at least 1.");
            }

            Isolate = config.ExceptionPolicy == ExceptionPolicy.Isolate;
            MaxDispatchDepth = config.MaxDispatchDepth;
            InitialListenerCapacity = Math.Max(1, config.InitialListenerCapacity);
            ErrorSink = errorSink ?? new DebugLogEventErrorSink();
            Trace = config.TracingEnabled ? trace : null;
            _ownerThreadId = Thread.CurrentThread.ManagedThreadId;

            lock (s_IndexLock)
            {
                _index = s_FreeIndices.Count > 0 ? s_FreeIndices.Pop() : s_NextIndex++;
            }
        }

        public IEvent<TData> Channel<TData>() where TData : struct
        {
            EnsureUsable();
            return ChannelSlot<TData>.Get(_index) ?? CreateChannel<TData>();
        }

        public void Publish<TData>(in TData data) where TData : struct
        {
            // Before the slot read: a disposed bus's index may already belong to a live bus, whose channel sits there.
            EnsureUsable();
            var channel = ChannelSlot<TData>.Get(_index);
            if (channel != null)
            {
                channel.Publish(in data);
                return;
            }

            // Nobody has ever subscribed to TData on this bus: nothing to deliver, no channel created.
            Trace?.OnPublished(in data, 0);
        }

        public IDisposable Subscribe<TData>(EventListener<TData> listener) where TData : struct
        {
            return Channel<TData>().Subscribe(listener);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            for (var i = 0; i < _channels.Count; i++)
            {
                _channels[i].Close(_index);
            }

            _channels.Clear();
            lock (s_IndexLock)
            {
                s_FreeIndices.Push(_index);
            }
        }

        // ---- called by the channels ------------------------------------------------------------------------------

        internal void EnsureUsable()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(EventBus));
            }

            EnsureOwnerThread();
        }

        internal void EnterDispatch()
        {
            EnsureUsable();
            if (_depth >= MaxDispatchDepth)
            {
                throw new InvalidOperationException(
                    $"EventBus dispatch depth exceeded {MaxDispatchDepth}: a listener keeps publishing (event loop?).");
            }

            _depth++;
        }

        internal void ExitDispatch()
        {
            _depth--;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void EnsureOwnerThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != _ownerThreadId)
            {
                throw new InvalidOperationException("EventBus is main-thread only: it was used from a thread other than the one that created it.");
            }
        }

        private Channel<TData> CreateChannel<TData>() where TData : struct
        {
            var channel = new Channel<TData>(this);
            ChannelSlot<TData>.Set(_index, channel);
            _channels.Add(channel);
            return channel;
        }
    }
}
