using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Hospitality.Utilities
{
    public static class ThingCache
    {
        private static Dictionary<Map, ThingCacheSet> allCacheSets = new Dictionary<Map, ThingCacheSet>();

        public static ThingCacheSet GetSetFor(Map map) => allCacheSets.TryGetValue(map, null);

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
    }

    public class ThingCacheSet
    {
        //
        private static List<Thing> vendingMachines = new List<Thing>();
        //private static List<CompVendingMachine> vendingMachines = new List<CompVendingMachine>();

        public List<Thing> AllVendingMachines => vendingMachines;

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
