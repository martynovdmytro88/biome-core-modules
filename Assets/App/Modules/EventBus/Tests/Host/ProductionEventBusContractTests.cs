using System;
using System.Collections.Generic;
using System.Threading;
using Biome.EventBus.Abstract;
using Biome.EventBus.Config;
using Biome.EventBus.Data;
using Biome.EventBus.Editor.Tests;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is =UnityEngine.TestTools.Constraints.Is;

namespace Biome.EventBus.Tests.Host
{
    /// <summary>
    /// Binds the package's contract suite to the production bus, and proves what only the production bus does: the
    /// dispose guard (F1), the isolate policy and its sink (B2, B3), tracing (B7), the main-thread guard (B8), the
    /// minimal factory call (B9) and zero allocation on publish.
    /// </summary>
    [TestFixture]
    public sealed class ProductionEventBusContractTests : EventBusContractTests
    {
        private int _sink;

        protected override IEventBus CreateBus() => EventBusFactory.Create(new EventBusConfig());

        protected override IEventBus CreateRethrowBus() => EventBusFactory.Create(new EventBusConfig { ExceptionPolicy = ExceptionPolicy.Rethrow });

        // ---- F1 dispose: a stale bus never reaches the bus that reused its index ---------------------------------

        [Test]
        public void F1_DisposedBus_PublishThrows_AndNeverReachesTheBusThatReusedItsIndex()
        {
            IEventBus a = NewBus();
            a.Dispose();

            // The second bus takes the first one's freed index, so its channel sits in the first one's slot.
            IEventBus b = NewBus();
            var inB = 0;
            b.Subscribe((in TestDamageDealt _) => inB++);

            Assert.Throws<ObjectDisposedException>(() => a.Publish(new TestDamageDealt(1)));
            Assert.That(inB, Is.EqualTo(0), "the disposed bus must not publish into the live one");
        }

        // ---- B2 Isolate (default) -----------------------------------------------------------------------------

        [Test]
        public void B2_Isolate_OtherListenersStillReceive_ExceptionGoesToTheSink()
        {
            var sink = new RecordingErrorSink();
            IEventBus bus = EventBusFactory.Create(new EventBusConfig(), sink);
            try
            {
                var received = 0;
                bus.Subscribe((in TestDamageDealt _) => received++);
                bus.Subscribe((in TestDamageDealt _) => throw new FormatException("listener failed"));
                bus.Subscribe((in TestDamageDealt _) => received++);

                Assert.DoesNotThrow(() => bus.Publish(new TestDamageDealt(1)));

                Assert.That(received, Is.EqualTo(2));
                Assert.That(sink.Received.Count, Is.EqualTo(1));
                Assert.That(sink.Received[0].Exception, Is.TypeOf<FormatException>());
                Assert.That(sink.Received[0].DataType, Is.EqualTo(typeof(TestDamageDealt)));
            }
            finally
            {
                bus.Dispose();
            }
        }

        [Test]
        public void B3_Isolate_LoopGuardExceptionIsReportedToTheSink()
        {
            var sink = new RecordingErrorSink();
            IEventBus bus = EventBusFactory.Create(new EventBusConfig { MaxDispatchDepth = 4 }, sink);
            try
            {
                var deliveries = 0;
                bus.Subscribe((in TestDamageDealt e) =>
                {
                    deliveries++;
                    bus.Publish(new TestDamageDealt(e.Amount + 1));
                });

                Assert.DoesNotThrow(() => bus.Publish(new TestDamageDealt(0)));

                Assert.That(deliveries, Is.EqualTo(4), "depths 1..MaxDispatchDepth delivered, the next one refused");
                Assert.That(sink.Received.Count, Is.EqualTo(1));
                Assert.That(sink.Received[0].Exception, Is.TypeOf<InvalidOperationException>());
            }
            finally
            {
                bus.Dispose();
            }
        }

        // ---- B7 tracing ----------------------------------------------------------------------------------------

        [Test]
        public void B7_Tracing_Off_TraceIsNeverCalled()
        {
            var trace = new CountingTrace();
            IEventBus bus = EventBusFactory.Create(new EventBusConfig(), null, trace);
            try
            {
                bus.Subscribe((in TestDamageDealt _) => { });
                bus.Publish(new TestDamageDealt(1));
                bus.Publish(new TestUnitSpawned(1));

                Assert.That(trace.Calls, Is.Empty);
            }
            finally
            {
                bus.Dispose();
            }
        }

        [Test]
        public void B7_Tracing_On_EveryPublishIsReportedWithItsListenerCount()
        {
            var trace = new CountingTrace();
            IEventBus bus = EventBusFactory.Create(new EventBusConfig { TracingEnabled = true }, null, trace);
            try
            {
                bus.Subscribe((in TestDamageDealt _) => { });
                bus.Subscribe((in TestDamageDealt _) => { });
                bus.Publish(new TestDamageDealt(1));
                bus.Publish(new TestUnitSpawned(1));

                Assert.That(trace.Calls, Is.EqualTo(new[] { (typeof(TestDamageDealt), 2), (typeof(TestUnitSpawned), 0) }));
            }
            finally
            {
                bus.Dispose();
            }
        }

        // ---- B8 main thread ------------------------------------------------------------------------------------

        [Test]
        public void B8_UseFromAnotherThread_ThrowsInvalidOperation()
        {
            IEventBus bus = NewBus();
            bus.Subscribe((in TestDamageDealt _) => { });
            Exception fromPublish = null;
            Exception fromSubscribe = null;

            var worker = new Thread(() =>
            {
                try
                {
                    bus.Publish(new TestDamageDealt(1));
                }
                catch (Exception e)
                {
                    fromPublish = e;
                }

                try
                {
                    bus.Subscribe((in TestUnitSpawned _) => { });
                }
                catch (Exception e)
                {
                    fromSubscribe = e;
                }
            });
            worker.Start();
            worker.Join();

            Assert.That(fromPublish, Is.TypeOf<InvalidOperationException>());
            Assert.That(fromSubscribe, Is.TypeOf<InvalidOperationException>());
        }

        // ---- B9 minimal set ------------------------------------------------------------------------------------

        [Test]
        public void B9_Create_WithNothingButAConfig_Works()
        {
            using (IEventBus bus = EventBusFactory.Create(new EventBusConfig()))
            {
                var received = 0;
                bus.Subscribe((in TestDamageDealt e) => received += e.Amount);
                bus.Publish(new TestDamageDealt(5));

                Assert.That(received, Is.EqualTo(5));
            }

            Assert.Throws<ArgumentNullException>(() => EventBusFactory.Create(null));
        }

        // ---- Zero allocation (the no-boxing proof) -------------------------------------------------------------

        [Test]
        public void ZeroAlloc_Publish_WithListeners_AllocatesNothingAfterWarmUp()
        {
            IEventBus bus = NewBus();
            IEvent<TestSnapshotTaken> channel = bus.Channel<TestSnapshotTaken>();
            bus.Subscribe((in TestDamageDealt e) => _sink += e.Amount);
            bus.Subscribe((in TestDamageDealt e) => _sink -= e.Amount);
            channel.Subscribe((in TestSnapshotTaken e) => _sink += (int)e.H);
            var tiny = new TestDamageDealt(3);
            var large = new TestSnapshotTaken(1);

            void PublishBatch()
            {
                for (var i = 0; i < 100; i++)
                {
                    bus.Publish(in tiny);
                    bus.Publish(in large);
                    channel.Publish(in large);
                }
            }

            // The warm-up fills the JIT and generic-dispatch caches.
            PublishBatch();

            // Unity's allocation constraint records GC.Alloc on the calling thread.
            Assert.That(PublishBatch, Is.Not.AllocatingGCMemory());
        }

        private sealed class RecordingErrorSink : IEventErrorSink
        {
            public readonly List<(Exception Exception, Type DataType)> Received = new List<(Exception, Type)>();

            public void OnListenerException(Exception exception, Type dataType) => Received.Add((exception, dataType));
        }

        private sealed class CountingTrace : IEventTrace
        {
            public readonly List<(Type DataType, int ListenerCount)> Calls = new List<(Type, int)>();

            public void OnPublished<TData>(in TData data, int listenerCount)
                where TData : struct => Calls.Add((typeof(TData), listenerCount));
        }
    }
}
