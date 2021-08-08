using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace Hospitality 
{
    /// <summary>
    /// Loaded via xml. Added so guests are upset when they can't afford a bed.
    /// </summary>
    public class ThoughtWorker_CantAffordBed : ThoughtWorker
    {
        public override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            if (pawn == null) return ThoughtState.Inactive;
            if (pawn.thingIDNumber == 0) return ThoughtState.Inactive; // What do you know!!!

            if (Current.ProgramState != ProgramState.Playing)
            {
                return ThoughtState.Inactive;
            }
            if (!pawn.IsArrivedGuest(out var compGuest)) return ThoughtState.Inactive;

            if(compGuest.rescued) return ThoughtState.Inactive;
            if(compGuest.HasBed) return ThoughtState.Inactive;
            
            var silver = pawn.inventory.innerContainer.FirstOrDefault(i => i.def == ThingDefOf.Silver);
            var money = silver?.stackCount ?? 0;

            var beds = pawn.MapHeld.GetGuestBeds(pawn.GetGuestArea()).ToArray();
            if(beds.Length == 0) return ThoughtState.Inactive;

            if (!beds.Any(bed => bed.AnyUnoccupiedSleepingSlot)) return ThoughtState.Inactive;
            if (beds.Any(bed => bed.rentalFee <= money && bed.AnyUnownedSleepingSlot)) return ThoughtState.Inactive;

            return true;
        }
    }
}