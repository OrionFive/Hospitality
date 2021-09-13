using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hospitality.Utilities;
using RimWorld;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public class WorkGiver_EmptyVendingMachine : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => ThingCache.GetSetFor(pawn.Map).AllVendingMachines;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var vendingMachine = t.TryGetComp<CompVendingMachine>();
            return vendingMachine != null && vendingMachine.ShouldEmpty && pawn.CanReserve(t, 1, 1);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var vendingMachine = t.TryGetComp<CompVendingMachine>();
            var silver = vendingMachine?.GetDirectlyHeldThings()?.FirstOrDefault();
            if (silver != null)
            {
                if (StoreUtility.TryFindBestBetterStorageFor(silver, pawn, pawn.Map, StoreUtility.CurrentStoragePriorityOf(silver), pawn.Faction, out _, out _))
                {
                    return JobMaker.MakeJob(HospitalityDefOf.VendingMachine_EmptyVendingMachine, t, silver);
                }
            }
            return null;
        }
    }
}
