using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using NUnit.Framework;

namespace AutofacContrib.NSubstitute.Tests
{
    public interface IThing<T>
    {
        bool TestThing(T args);
    }

    public class MagicPayload
    {
        public int Value { get; }

        public MagicPayload(int v)
        {
            Value = v;
        }
    }

    public class RealThing : IThing<MagicPayload>
    {
        public bool TestThing(MagicPayload args)
        {
            return args.Value == 999;
        }
    }

    public class ThingRunner<T>
    {
        public IReadOnlyList<IThing<T>> Things { get; }

        public ThingRunner(IReadOnlyList<IThing<T>> things)
        {
            Things = things;
        }

        public void TestRunner(T args)
        {
            var matching = Things.Where(t => t.TestThing(args)).ToArray();
            if (matching.Length == 0)
            {
                throw new InvalidOperationException("Oops");
            }
        }
    }

    public class ThingHandler
    {
        public ThingRunner<MagicPayload> Runner { get; }

        public ThingHandler(ThingRunner<MagicPayload> runner)
        {
            Runner = runner;
        }

        public void RunHandler()
        {
            var args = new MagicPayload(999);
            Runner.TestRunner(args);
        }
    }

    class ClosedGenericTypeTests
    {
        [Test]
        public void Given_some_over_engineered_registration_When_registered_as_closed_generic_type_Then_resolution_is_successful()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ThingHandler>();
            builder.RegisterGeneric(typeof(ThingRunner<>));
            builder.RegisterType<RealThing>().As<IThing<MagicPayload>>();

            var container = builder.Build();

            var real = container.Resolve<IThing<MagicPayload>>();
            Assert.True(real.TestThing(new MagicPayload(999)));

            var handler = container.Resolve<ThingHandler>();
            Assert.IsInstanceOf<ThingRunner<MagicPayload>>(handler.Runner);
            Assert.That(handler.Runner.Things.Count == 1);
            handler.RunHandler();

            using (var s = new AutoSubstitute(builder2 =>
            {
                builder2.RegisterType<MagicPayload>().AsSelf();
                builder2.RegisterType<ThingHandler>();
                builder2.RegisterGeneric(typeof(ThingRunner<>));
                builder2.RegisterType<RealThing>().As<IThing<MagicPayload>>();
            }))
            {
                s.Provide<IThing<MagicPayload>>(new RealThing());

                var r2 = s.ResolveAndSubstituteFor<IThing<MagicPayload>>();
                Assert.IsInstanceOf<RealThing>(r2);
                Assert.IsTrue(r2.TestThing(new MagicPayload(999)));

                var h2 = s.Resolve<ThingHandler>();
                h2.RunHandler();
            }
        }
    }
}
