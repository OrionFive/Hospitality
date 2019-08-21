using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public class JobGiver_ClaimBed : ThinkNode_JobGiver
    {
        private JobDef JobDef => DefDatabase<JobDef>.GetNamed("ClaimGuestBed");

        protected override Job TryGiveJob(Pawn guest)
        {
            var guestComp = guest.GetComp<CompGuest>();
            if (guestComp == null) return null;
            if (guestComp.HasBed) return null;

            if (GenTicks.TicksGame < guestComp.lastBedCheckTick + 2500) return null;
            
            guestComp.lastBedCheckTick = GenTicks.TicksGame;

            
            var silver = guest.inventory.innerContainer.FirstOrDefault(i => i.def == ThingDefOf.Silver);
            var money = silver?.stackCount ?? 0;

            var beds = FindAvailableBeds(guest, money);
            Log.Message($"Found {beds.Length} guest beds that {guest.LabelShort} can afford (<= {money} silver).");
            if (!beds.Any()) return null;

            var bed = beds.RandomElement();
            return new Job(JobDef, bed) {takeExtraIngestibles = bed.rentalFee}; // Store rentalFee to avoid cheating
        }

        private static Building_GuestBed[] FindAvailableBeds(Pawn guest, int money)
        {
            var beds = guest.MapHeld.GetGuestBeds(guest.GetGuestArea());
            return beds.Where(bed => bed.AnyUnownedSleepingSlot && bed.rentalFee <= money).ToArray();
        }
    }
}
