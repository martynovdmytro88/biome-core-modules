using System;
using Biome.EventBus.Abstract;

namespace Biome.EventBus
{
    /// <summary>Lets the bus close its channels without knowing their TData.</summary>
    internal abstract class ChannelBase
    {
        internal abstract void Close(int busIndex);
    }

    /// <summary>
    /// The per-TData channel table: bus index → that bus's channel. Written only when a channel is created or its bus
    /// is disposed; read on every bus-level Publish.
    /// </summary>
    internal static class ChannelSlot<TData> where TData : struct
    {
        private static Channel<TData>[] s_ByBus = new Channel<TData>[4];

        internal static Channel<TData> Get(int busIndex)
        {
            var byBus = s_ByBus;
            return busIndex < byBus.Length ? byBus[busIndex] : null;
        }

        internal static void Set(int busIndex, Channel<TData> channel)
        {
            if (busIndex >= s_ByBus.Length)
            {
                Array.Resize(ref s_ByBus, Math.Max(busIndex + 1, s_ByBus.Length * 2));
            }

            s_ByBus[busIndex] = channel;
        }
    }

    /// <summary>
    /// One typed channel. Listeners live in an array walked by index up to the count taken at the start of Publish, so
    /// a listener added mid-dispatch waits for the next Publish. Removal clears the subscription's listener at once
    /// (never called again) and compacts the array only when no dispatch of this channel is in flight.
    /// </summary>
    internal sealed class Channel<TData> : ChannelBase, IEvent<TData> where TData : struct
    {
        private readonly EventBus _bus;
        private Subscription[] _items;
        private int _count;     // used slots, including cleared ones awaiting compaction
        private int _live;      // listeners still subscribed
        private int _dispatching;

        internal Channel(EventBus bus)
        {
            _bus = bus;
            _items = new Subscription[bus.InitialListenerCapacity];
        }

        public void Publish(in TData data)
        {
            _bus.EnterDispatch();
            var items = _items;
            var count = _count;
            _dispatching++;
            try
            {
                _bus.Trace?.OnPublished(in data, _live);
                if (_bus.Isolate)
                {
                    for (var i = 0; i < count; i++)
                    {
                        var listener = items[i].Listener;
                        if (listener == null)
                        {
                            continue;
                        }

                        try
                        {
                            listener(in data);
                        }
                        catch (Exception exception)
                        {
                            _bus.ErrorSink.OnListenerException(exception, typeof(TData));
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        items[i].Listener?.Invoke(in data);
                    }
                }
            }
            finally
            {
                _dispatching--;
                _bus.ExitDispatch();
                if (_dispatching == 0 && _live != _count)
                {
                    Compact();
                }
            }
        }

        public IDisposable Subscribe(EventListener<TData> listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            _bus.EnsureUsable();
            if (_count == _items.Length)
            {
                // A fresh array: an in-flight Publish keeps walking the old one, whose first `count` slots are unchanged.
                var grown = new Subscription[_items.Length * 2];
                Array.Copy(_items, grown, _count);
                _items = grown;
            }

            var subscription = new Subscription(this, listener);
            _items[_count++] = subscription;
            _live++;
            return subscription;
        }

        internal override void Close(int busIndex)
        {
            for (var i = 0; i < _count; i++)
            {
                _items[i].Listener = null;
            }

            _live = 0;
            if (_dispatching == 0)
            {
                Array.Clear(_items, 0, _count);
                _count = 0;
            }

            ChannelSlot<TData>.Set(busIndex, null);
        }

        private void Remove(Subscription subscription)
        {
            subscription.Listener = null;
            _live--;
            if (_dispatching == 0)
            {
                Compact();
            }
        }

        private void Compact()
        {
            // Never runs mid-dispatch, so the array can be edited in place; order is not part of the contract.
            var write = 0;
            for (var read = 0; read < _count; read++)
            {
                if (_items[read].Listener != null)
                {
                    _items[write++] = _items[read];
                }
            }

            Array.Clear(_items, write, _count - write);
            _count = write;
        }

        private sealed class Subscription : IDisposable
        {
            private readonly Channel<TData> _channel;
            internal EventListener<TData> Listener;

            internal Subscription(Channel<TData> channel, EventListener<TData> listener)
            {
                _channel = channel;
                Listener = listener;
            }

            public void Dispose()
            {
                if (Listener != null)
                {
                    _channel.Remove(this);
                }
            }
        }
    }
}
