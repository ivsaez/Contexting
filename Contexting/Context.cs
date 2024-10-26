using AgentBody;
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
        private readonly HashSet<IIdentifiable> agents;
        private readonly HashSet<IIdentifiable> items;

        public A? Main { get; }
        public M? Mapped { get; }

        public ISet<IIdentifiable> Destinations => destinations;
        public ISet<IIdentifiable> Agents => agents;
        public ISet<IIdentifiable> Items => items;

        public ISet<IIdentifiable> All =>
            new HashSet<IIdentifiable>()
                .Union(destinations)
                .Union(agents)
                .Union(items)
                .ToHashSet();

        public static Context<A, I, M> FromAgent(A main, Existents<A, I, M> existents) =>
            new Context<A, I, M>(main, default(M), existents);

        public static Context<A, I, M> FromPlace(M mapped, Existents<A, I, M> existents) =>
            new Context<A, I, M>(default(A), mapped, existents);

        public void Add(Context<A, I, M> context)
        {
            foreach (var mapped in context.destinations)
                destinations.Add(mapped);

            foreach (var agent in context.agents)
                agents.Add(agent);

            foreach (var item in context.items)
                items.Add(item);
        }

        private Context(A? main, M? mapped, Existents<A, I, M> existents)
        {
            destinations = new HashSet<IIdentifiable>();
            agents = new HashSet<IIdentifiable>();
            items = new HashSet<IIdentifiable>();

            Main = main;
            Mapped = mapped is null
                ? Main is not null
                    ? existents.GetUbication(Main)
                    : default(M)
                : mapped;

            buildFocusedContext(existents);
        }

        private void buildFocusedContext(Existents<A, I, M> existents)
        {
            extractDestinations(existents);

            var others = extractAgents(existents);
            extractOthersCarried(others, existents);
            extractOthersWear(others, existents);

            extractItems(existents);

            extractMainItems(existents);
        }

        private void extractDestinations(Existents<A, I, M> existents)
        {
            var exits = existents.GetMappeds(Mapped!.Exits.All.ToArray());
            foreach (var exit in exits)
                destinations.Add(exit);

            destinations.Add(Mapped);
        }

        private IEnumerable<A> extractAgents(Existents<A, I, M> existents)
        {
            var agents = Main is null
                ? existents.GetAgents(Mapped!.Agents.All.ToArray())
                : existents.GetAgents(Mapped!.Agents.OthersThan(Main).ToArray());

            foreach (var agent in agents)
                this.agents.Add(agent);

            return agents;
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

        private void extractItems(Existents<A, I, M> existents)
        {
            var items = Mapped!.Items.AllAccessible(existents.ItemsRepository);
            foreach (var item in items)
                this.items.Add(item);
        }

        private void extractMainItems(Existents<A, I, M> existents)
        {
            if(Main is not null)
            {
                extractCarriedMainItems(existents);
                extractWearedMainItems(existents);
            }
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