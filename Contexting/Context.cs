﻿using AgentBody;
using Agents;
using Identification;
using Instanciation;
using Items;
using Mapping;
using Saver;
using Wearing;

namespace Contexting
{
    public class Context<A, I, M>
        where A : IAgent, ISavable, ICloneable
        where I : IItem, ISavable, ICloneable
        where M : IMapped, ISavable, ICloneable
    {
        private readonly HashSet<IIdentifiable> destinations;
        private readonly HashSet<IIdentifiable> others;
        private readonly HashSet<IIdentifiable> items;

        public A Main { get; }

        public ISet<IIdentifiable> Destinations => destinations;
        public ISet<IIdentifiable> Others => others;
        public ISet<IIdentifiable> Items => items;

        public ISet<IIdentifiable> All =>
            new HashSet<IIdentifiable>()
                .Union(destinations)
                .Union(others)
                .Union(items)
                .ToHashSet();

        public Context(A main, Existents<A, I, M> existents)
        {
            Main = main;

            destinations = new HashSet<IIdentifiable>();
            others = new HashSet<IIdentifiable>();
            items = new HashSet<IIdentifiable>();

            buildContext(existents);
        }

        private void buildContext(Existents<A, I, M> existents)
        {
            var mapped = existents.GetUbication(Main);

            extractDestinations(mapped, existents);

            var others = extractOthers(mapped, existents);
            extractOthersCarried(others, existents);
            extractOthersWear(others, existents);

            extractItems(mapped, existents);

            extractMainItems(existents);
        }

        private void extractDestinations(M mapped, Existents<A, I, M> existents)
        {
            var exits = existents.GetMappeds(mapped.Exits.All.ToArray());
            foreach (var exit in exits)
                destinations.Add(exit);

            destinations.Add(mapped);
        }

        private IEnumerable<A> extractOthers(M mapped, Existents<A, I, M> existents)
        {
            var others = existents.GetAgents(mapped.Agents.OthersThan(Main).ToArray());

            foreach (var other in others)
                this.others.Add(other);

            return others;
        }

        private void extractOthersCarried(IEnumerable<A> others, Existents<A, I, M> existents)
        {
            foreach (var carrier in others.Where(o => o is ICarrier).OfType<ICarrier>())
            {
                foreach (var carried in carrier.Carrier.GetCarrieds(existents.ItemsRepository).Everything)
                    items.Add(carried);
            }
        }

        private void extractOthersWear(IEnumerable<A> others, Existents<A, I, M> existents)
        {
            foreach (var weared in others.Where(o => o is IWeared).OfType<IWeared>())
            {
                foreach (var cloth in weared.Wear.GetVisibleCloths(existents.ItemsRepository))
                    items.Add(cloth);
            }
        }

        private void extractItems(M mapped, Existents<A, I, M> existents)
        {
            var items = mapped.Items.AllAccessible(existents.ItemsRepository);
            foreach (var item in items)
                this.items.Add(item);
        }

        private void extractMainItems(Existents<A, I, M> existents)
        {
            extractCarriedMainItems(existents);

            extractWearedMainItems(existents);
        }

        private void extractCarriedMainItems(Existents<A, I, M> existents)
        {
            if (Main is ICarrier)
            {
                var carrier = (ICarrier)Main;
                var carrieds = carrier.Carrier.GetCarrieds(existents.ItemsRepository);
                foreach (var item in carrieds.Everything)
                    this.items.Add(item);

                if (carrieds.Back is not null)
                {
                    foreach (var item in carrieds.Back.Inventory.AllObservables(existents.ItemsRepository))
                        this.items.Add(item);
                }
            }
        }

        private void extractWearedMainItems(Existents<A, I, M> existents)
        {
            if (Main is IWeared)
            {
                var wearer = (IWeared)Main;
                foreach (var cloth in wearer.Wear.GetVisibleCloths(existents.ItemsRepository))
                    this.items.Add(cloth);
            }
        }
    }
}