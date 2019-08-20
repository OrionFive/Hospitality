using System.Linq;
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
            if (guestComp.bed != null && guestComp.bed.owners.Contains(guest)) return null;

            if (GenTicks.TicksGame < guestComp.lastBedCheckTick + 500) return null;
            
            guestComp.lastBedCheckTick = GenTicks.TicksGame;

            var beds = FindAvailableBeds(guest);
            Log.Message($"Found {beds.Length} guest beds.");
            if (!beds.Any()) return null;

            var bed = beds.RandomElement();
            Log.Message($"{guest.LabelShort} got ClaimGuestBed job on {bed.Label}.");
            return new Job(JobDef, bed);
        }

        private static Building_GuestBed[] FindAvailableBeds(Pawn guest)
        {
            var beds = guest.MapHeld.GetGuestBeds(guest.GetGuestArea());
            return beds.Where(bed => bed.AnyUnownedSleepingSlot).ToArray();
        }
    }
}
