using System.Collections.Generic;
using Verse;

namespace Hospitality.Utilities
{
    public static class ThingCache
    {
        private static readonly Dictionary<Map, ThingCacheSet> allCacheSets = new Dictionary<Map, ThingCacheSet>();

        public static ThingCacheSet GetSetFor(Map map) => allCacheSets.TryGetValue(map);

        public static void TryRegisterNewThing(Thing thing)
        {
            if (!allCacheSets.ContainsKey(thing.Map))
            {
                allCacheSets.Add(thing.Map, new ThingCacheSet());
            }

            allCacheSets[thing.Map].TryRegister(thing);
        }

        public static void TryDeregister(Thing thing, Map oldMap)
        {
            if (allCacheSets.ContainsKey(oldMap))
            {
                allCacheSets[oldMap].Deregister(thing);
            }
        }

        public class ThingCacheSet
        {
            private readonly List<Thing> vendingMachines = new List<Thing>();
            public IEnumerable<Thing> AllVendingMachines => vendingMachines;

            public void TryRegister(Thing newThing)
            {
                if (newThing is ThingWithComps thingWithComps)
                {
                    if (thingWithComps.TryGetComp<CompVendingMachine>() != null)
                    {
                        vendingMachines.Add(newThing);
                    }
                }
            }

            public void Deregister(Thing thing)
            {
                vendingMachines.Remove(thing);
            }
        }
    }
}
