using System;
using System.Collections.Generic;
using Biome.EventBus.Abstract;

namespace Biome.EventBus.Fakes
{
    /// <summary>
    /// The recording fake of <see cref="IEventBus"/> — ships with the contract so a consumer's tests need only the
    /// Abstract reference. Delivers like the production bus (synchronous · depth-first re-entrancy with a loop guard ·
    /// (un)subscribe during dispatch · dispose semantics) and records every published value per type. Fail-fast: the
    /// first listener exception propagates out of Publish. No tracing, no thread check. Not for the hot path.
    /// </summary>
    public sealed class RecordingEventBus : IEventBus
    {
        /// <summary>Deepest allowed Publish-from-a-listener nesting; the production default.</summary>
        public const int MaxDispatchDepth = 16;

        private readonly Dictionary<Type, IRecordingChannel> _channels = new Dictionary<Type, IRecordingChannel>();
        private int _depth;
        private bool _disposed;

        public RecordingEventBus()
        {
        }

        /// <summary>Every value published as <typeparamref name="TData"/> since construction or the last <see cref="Clear"/>, in publish order.</summary>
        public IReadOnlyList<TData> Published<TData>() where TData : struct
        {
            return _channels.TryGetValue(typeof(TData), out var channel)
                ? ((RecordingChannel<TData>)channel).Recorded
                : (IReadOnlyList<TData>)Array.Empty<TData>();
        }

        public int Count<TData>() where TData : struct => Published<TData>().Count;

        /// <summary>Forgets the recorded values; subscriptions stay.</summary>
        public void Clear()
        {
            foreach (var channel in _channels.Values)
            {
                channel.ClearRecorded();
            }
        }

        public IEvent<TData> Channel<TData>() where TData : struct
        {
            EnsureNotDisposed();
            if (!_channels.TryGetValue(typeof(TData), out var channel))
            {
                channel = new RecordingChannel<TData>(this);
                _channels.Add(typeof(TData), channel);
            }

            return (RecordingChannel<TData>)channel;
        }

        public void Publish<TData>(in TData data) where TData : struct => Channel<TData>().Publish(in data);

        public IDisposable Subscribe<TData>(EventListener<TData> listener) where TData : struct => Channel<TData>().Subscribe(listener);

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (var channel in _channels.Values)
            {
                channel.Close();
            }
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(RecordingEventBus));
            }
        }

        private interface IRecordingChannel
        {
            void ClearRecorded();
            void Close();
        }

        private sealed class RecordingChannel<TData> : IEvent<TData>, IRecordingChannel where TData : struct
        {
            private readonly RecordingEventBus _bus;
            private readonly List<Subscription> _listeners = new List<Subscription>();

            internal readonly List<TData> Recorded = new List<TData>();

            internal RecordingChannel(RecordingEventBus bus)
            {
                _bus = bus;
            }

            public void Publish(in TData data)
            {
                _bus.EnsureNotDisposed();
                if (_bus._depth >= MaxDispatchDepth)
                {
                    throw new InvalidOperationException(
                        $"RecordingEventBus dispatch depth exceeded {MaxDispatchDepth}: a listener keeps publishing (event loop?).");
                }

                Recorded.Add(data);
                var snapshot = _listeners.ToArray(); // a listener added mid-dispatch waits for the next Publish
                _bus._depth++;
                try
                {
                    foreach (var subscription in snapshot)
                    {
                        subscription.Listener?.Invoke(in data);
                    }
                }
                finally
                {
                    _bus._depth--;
                }
            }

            public IDisposable Subscribe(EventListener<TData> listener)
            {
                if (listener == null)
                {
                    throw new ArgumentNullException(nameof(listener));
                }

                _bus.EnsureNotDisposed();
                var subscription = new Subscription(this, listener);
                _listeners.Add(subscription);
                return subscription;
            }

            public void ClearRecorded() => Recorded.Clear();

            public void Close()
            {
                foreach (var subscription in _listeners)
                {
                    subscription.Listener = null;
                }

                _listeners.Clear();
            }

            private sealed class Subscription : IDisposable
            {
                private readonly RecordingChannel<TData> _channel;
                internal EventListener<TData> Listener;

                internal Subscription(RecordingChannel<TData> channel, EventListener<TData> listener)
                {
                    _channel = channel;
                    Listener = listener;
                }

                public void Dispose()
                {
                    if (Listener == null)
                    {
                        return;
                    }

                    Listener = null;
                    _channel._listeners.Remove(this);
                }
            }
        }
    }
}
