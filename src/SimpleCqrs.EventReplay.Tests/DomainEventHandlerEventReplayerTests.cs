﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Should;
using SimpleCqrs.Eventing;
using SimpleCqrs.Unity;

namespace SimpleCqrs.EventReplay.Tests
{
    [TestClass]
    public class DomainEventHandlerEventReplayerTests
    {
        private TestRuntime runtime;

        private Mock<IEventStore> fakeEventStore;

        [TestInitialize]
        public void Setup()
        {
            fakeEventStore = new Mock<IEventStore>();

            runtime = new TestRuntime(fakeEventStore.Object);
            runtime.Start();
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (runtime != null)
                runtime.Shutdown();
        }

        [TestMethod]
        public void Replays_events_when_there_is_one_handler_and_one_event()
        {
            var domainEvent = new AppleDomainEvent();
            var appleDomainEventHandler = new AppleDomainEventHandler();

            ReturnTheseEventsForTheseTypes(new[] {domainEvent},
                                           new[] {typeof (AppleDomainEvent)});

            runtime.ServiceLocator.Register(appleDomainEventHandler);

            ReplayEventsForThisHandler<AppleDomainEventHandler>();

            appleDomainEventHandler.HandledEvents.Single().ShouldBeSameAs(domainEvent);
        }

        private void ReplayEventsForThisHandler<T>() where T : class
        {
            var domainEventHandlerEventReplayer = new DomainEventHandlerEventReplayer(fakeEventStore.Object, runtime.ServiceLocator);
            domainEventHandlerEventReplayer.ReplayEventsForThisHandler<T>();
        }

        private void ReturnTheseEventsForTheseTypes(IEnumerable<DomainEvent> domainEvents, Type[] types)
        {
            fakeEventStore.Setup(x => x.GetEventsOfTheseTypes(It.IsAny<IEnumerable<Type>>()))
                .Returns((IEnumerable<Type> t) =>
                             {
                                 if (t.Count() == types.Count() && t.Any(x => types.Contains(x) == false) == false)
                                     return domainEvents;
                                 return new DomainEvent[] {};
                             });
        }

        public class AppleDomainEvent : DomainEvent
        {
        }

        public class AppleDomainEventHandler : BaseDomainEventHandler, IHandleDomainEvents<AppleDomainEvent>
        {
            public void Handle(AppleDomainEvent domainEvent)
            {
                HandledEvents.Add(domainEvent);
            }
        }

        public class BaseDomainEventHandler
        {
            public BaseDomainEventHandler()
            {
                HandledEvents = new List<DomainEvent>();
            }

            public IList<DomainEvent> HandledEvents { get; set; }
        }

        public class TestRuntime : SimpleCqrsRuntime<UnityServiceLocator>
        {
            private readonly IEventStore eventStore;

            public TestRuntime(IEventStore eventStore)
            {
                this.eventStore = eventStore;
            }

            protected override IEventStore GetEventStore(IServiceLocator serviceLocator)
            {
                return eventStore;
            }
        }
    }
}