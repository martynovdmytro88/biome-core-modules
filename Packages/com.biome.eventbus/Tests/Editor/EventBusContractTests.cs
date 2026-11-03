using System;
using System.Collections.Generic;
using Biome.EventBus.Abstract;
using NUnit.Framework;

namespace Biome.EventBus.Editor.Tests
{
    /// <summary>
    /// The bus-behaviour contract (B1–B6), written once over <see cref="IEventBus"/>. The package binds it to the
    /// recording fake; a host binds its own bus by deriving and overriding the two factory hooks. Receivers are asserted
    /// as a set, never as an order (B5).
    /// </summary>
    public abstract class EventBusContractTests
    {
        private readonly List<IEventBus> _created = new List<IEventBus>();

        /// <summary>The bus under test with its default policy.</summary>
        protected abstract IEventBus CreateBus();

        /// <summary>A bus whose first listener exception propagates out of Publish.</summary>
        protected abstract IEventBus CreateRethrowBus();

        protected IEventBus NewBus() => Track(CreateBus());

        protected IEventBus NewRethrowBus() => Track(CreateRethrowBus());

        private IEventBus Track(IEventBus bus)
        {
            _created.Add(bus);
            return bus;
        }

        [TearDown]
        public void DisposeBuses()
        {
            foreach (IEventBus bus in _created)
            {
                bus.Dispose();
            }

            _created.Clear();
        }

        // ---- B1 delivery ---------------------------------------------------------------------------------------

        [Test]
        public void B1_Publish_DeliversToEveryListenerBeforeReturning()
        {
            IEventBus bus = NewBus();
            var received = new List<string>();
            bus.Subscribe((in TestDamageDealt e) => received.Add("ui:" + e.Amount));
            bus.Subscribe((in TestDamageDealt e) => received.Add("vfx:" + e.Amount));
            bus.Subscribe((in TestDamageDealt e) => received.Add("audio:" + e.Amount));

            bus.Publish(new TestDamageDealt(7));

            CollectionAssert.AreEquivalent(new[] { "ui:7", "vfx:7", "audio:7" }, received);
        }

        [Test]
        public void B1_ChannelAndBusLevel_AreTheSameDelivery()
        {
            IEventBus bus = NewBus();
            IEvent<TestDamageDealt> channel = bus.Channel<TestDamageDealt>();
            var total = 0;
            channel.Subscribe((in TestDamageDealt e) => total += e.Amount);
            bus.Subscribe((in TestDamageDealt e) => total += e.Amount * 100);

            bus.Publish(new TestDamageDealt(1));
            channel.Publish(new TestDamageDealt(2));

            Assert.That(total, Is.EqualTo(101 + 202));
            Assert.That(bus.Channel<TestDamageDealt>(), Is.SameAs(channel), "same channel instance per TData per bus");
        }

        [Test]
        public void B1_Publish_ReachesOnlyListenersOfThatType()
        {
            IEventBus bus = NewBus();
            var damage = 0;
            var spawned = 0;
            bus.Subscribe((in TestDamageDealt _) => damage++);
            bus.Subscribe((in TestUnitSpawned _) => spawned++);

            bus.Publish(new TestUnitSpawned(3));

            Assert.That((damage, spawned), Is.EqualTo((0, 1)));
        }

        [Test]
        public void B1_Publish_WithoutListeners_DoesNothing()
        {
            IEventBus bus = NewBus();

            Assert.DoesNotThrow(() => bus.Publish(new TestDamageDealt(1)));
            Assert.DoesNotThrow(() => bus.Channel<TestUnitSpawned>().Publish(new TestUnitSpawned(1)));
        }

        [Test]
        public void B1_LargePayload_ArrivesIntact()
        {
            IEventBus bus = NewBus();
            TestSnapshotTaken seen = default;
            bus.Subscribe((in TestSnapshotTaken e) => seen = e);

            bus.Publish(new TestSnapshotTaken(40));

            Assert.That((seen.A, seen.D, seen.H), Is.EqualTo((40L, 43L, 47L)));
        }

        [Test]
        public void B1_Subscribe_NullListener_Throws()
        {
            IEventBus bus = NewBus();

            Assert.Throws<ArgumentNullException>(() => bus.Subscribe<TestDamageDealt>(null));
        }

        // ---- B2 exceptions (fail-fast half; Isolate is implementation-side) --------------------------------------

        [Test]
        public void B2_Rethrow_ListenerExceptionPropagatesOutOfPublish()
        {
            IEventBus bus = NewRethrowBus();
            bus.Subscribe((in TestDamageDealt _) => throw new FormatException("listener failed"));

            FormatException thrown = Assert.Throws<FormatException>(() => bus.Publish(new TestDamageDealt(1)));
            Assert.That(thrown.Message, Is.EqualTo("listener failed"));

            // The bus stays usable after a propagated failure.
            var received = 0;
            bus.Subscribe((in TestUnitSpawned _) => received++);
            bus.Publish(new TestUnitSpawned(1));
            Assert.That(received, Is.EqualTo(1));
        }

        // ---- B3 re-entrancy ------------------------------------------------------------------------------------

        [Test]
        public void B3_PublishFromListener_IsDeliveredDepthFirst()
        {
            IEventBus bus = NewBus();
            var trail = new List<string>();
            bus.Subscribe((in TestDamageDealt e) =>
            {
                trail.Add("damage:before");
                bus.Publish(new TestUnitSpawned(e.Amount));
                trail.Add("damage:after");
            });
            bus.Subscribe((in TestUnitSpawned _) => trail.Add("spawned"));

            bus.Publish(new TestDamageDealt(1));

            Assert.That(trail, Is.EqualTo(new[] { "damage:before", "spawned", "damage:after" }));
        }

        [Test]
        public void B3_PublishLoop_ThrowsInvalidOperation()
        {
            IEventBus bus = NewRethrowBus();
            var deliveries = 0;
            bus.Subscribe((in TestDamageDealt e) =>
            {
                deliveries++;
                bus.Publish(new TestDamageDealt(e.Amount + 1));
            });

            Assert.Throws<InvalidOperationException>(() => bus.Publish(new TestDamageDealt(0)));
            Assert.That(deliveries, Is.GreaterThan(1).And.LessThan(1000), "bounded by the loop guard");

            // Depth unwinds: a later, non-looping Publish goes through.
            var ok = 0;
            bus.Subscribe((in TestUnitSpawned _) => ok++);
            bus.Publish(new TestUnitSpawned(1));
            Assert.That(ok, Is.EqualTo(1));
        }

        // ---- B4 (un)subscribe during dispatch ------------------------------------------------------------------

        [Test]
        public void B4_SubscribeDuringDispatch_WaitsForTheNextPublish()
        {
            IEventBus bus = NewBus();
            var late = 0;
            IDisposable lateSubscription = null;
            bus.Subscribe((in TestDamageDealt _) =>
            {
                if (lateSubscription == null)
                {
                    lateSubscription = bus.Subscribe((in TestDamageDealt __) => late++);
                }
            });

            bus.Publish(new TestDamageDealt(1));
            Assert.That(late, Is.EqualTo(0), "not called by the in-flight Publish");

            bus.Publish(new TestDamageDealt(2));
            Assert.That(late, Is.EqualTo(1), "called by the next one");
        }

        [Test]
        public void B4_SubscribeDuringDispatch_BeyondInitialCapacity_Holds()
        {
            IEventBus bus = NewBus();
            var late = 0;
            var subscribed = false;
            bus.Subscribe((in TestDamageDealt _) =>
            {
                if (subscribed)
                {
                    return;
                }

                subscribed = true;
                for (var i = 0; i < 20; i++)
                {
                    bus.Subscribe((in TestDamageDealt __) => late++);
                }
            });

            bus.Publish(new TestDamageDealt(1));
            bus.Publish(new TestDamageDealt(2));

            Assert.That(late, Is.EqualTo(20));
        }

        [Test]
        public void B4_UnsubscribeDuringDispatch_IsNeverCalledAgain()
        {
            // Each listener disposes the other; order is not guaranteed, so exactly one of them must run.
            IEventBus bus = NewBus();
            IDisposable first = null;
            IDisposable second = null;
            var calls = 0;
            first = bus.Subscribe((in TestDamageDealt _) =>
            {
                calls++;
                second.Dispose();
            });
            second = bus.Subscribe((in TestDamageDealt _) =>
            {
                calls++;
                first.Dispose();
            });

            bus.Publish(new TestDamageDealt(1));
            Assert.That(calls, Is.EqualTo(1));

            bus.Publish(new TestDamageDealt(2));
            Assert.That(calls, Is.EqualTo(2), "the survivor keeps receiving");
        }

        [Test]
        public void B4_DisposedSubscription_StopsDelivery_AndDoubleDisposeIsNoOp()
        {
            IEventBus bus = NewBus();
            var calls = 0;
            var kept = 0;
            IDisposable subscription = bus.Subscribe((in TestDamageDealt _) => calls++);
            bus.Subscribe((in TestDamageDealt _) => kept++);

            subscription.Dispose();
            Assert.DoesNotThrow(subscription.Dispose);
            bus.Publish(new TestDamageDealt(1));

            Assert.That((calls, kept), Is.EqualTo((0, 1)));
        }

        // ---- B6 lifetime ---------------------------------------------------------------------------------------

        [Test]
        public void B6_BusDispose_DropsSubscriptions_AndLaterUseThrows()
        {
            IEventBus bus = NewBus();
            IEvent<TestDamageDealt> channel = bus.Channel<TestDamageDealt>();
            var calls = 0;
            IDisposable subscription = bus.Subscribe((in TestDamageDealt _) => calls++);

            bus.Dispose();

            Assert.Throws<ObjectDisposedException>(() => bus.Publish(new TestDamageDealt(1)));
            Assert.Throws<ObjectDisposedException>(() => channel.Publish(new TestDamageDealt(1)));
            Assert.Throws<ObjectDisposedException>(() => bus.Subscribe((in TestDamageDealt _) => calls++));
            Assert.Throws<ObjectDisposedException>(() => bus.Channel<TestUnitSpawned>());
            Assert.DoesNotThrow(subscription.Dispose, "disposing a subscription after the bus = no-op");
            Assert.DoesNotThrow(bus.Dispose, "double bus Dispose = no-op");
            Assert.That(calls, Is.EqualTo(0));
        }

        [Test]
        public void B6_BusDispose_DuringDispatch_StopsTheRemainingListeners()
        {
            IEventBus bus = NewBus();
            var calls = 0;
            bus.Subscribe((in TestDamageDealt _) =>
            {
                calls++;
                bus.Dispose();
            });
            bus.Subscribe((in TestDamageDealt _) =>
            {
                calls++;
                bus.Dispose();
            });

            bus.Publish(new TestDamageDealt(1));

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void B6_Buses_AreIsolated_IncludingAfterADisposedOne()
        {
            IEventBus a = NewBus();
            IEventBus b = NewBus();
            var inA = 0;
            var inB = 0;
            a.Subscribe((in TestDamageDealt _) => inA++);
            b.Subscribe((in TestDamageDealt _) => inB++);

            a.Publish(new TestDamageDealt(1));
            Assert.That((inA, inB), Is.EqualTo((1, 0)));

            a.Dispose();
            // An implementation may reuse the slot of a.
            IEventBus c = NewBus();
            var inC = 0;
            c.Subscribe((in TestDamageDealt _) => inC++);
            c.Publish(new TestDamageDealt(1));

            Assert.That((inA, inB, inC), Is.EqualTo((1, 0, 1)), "a's listeners never leak into a later bus");
        }
    }
}
