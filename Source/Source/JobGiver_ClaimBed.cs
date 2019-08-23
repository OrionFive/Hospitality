using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using static UnityEngine.Mathf;

namespace Hospitality
{
    public class JobGiver_ClaimBed : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn guest)
        {
            var guestComp = guest.GetComp<CompGuest>();
            if (guestComp == null) return null;
            if (guestComp.HasBed) return null;

            if (GenTicks.TicksGame < guestComp.lastBedCheckTick + 600) return null;
            
            guestComp.lastBedCheckTick = GenTicks.TicksGame;

            var bed = guest.FindBedFor();
            if (bed == null) return null;

            return new Job(BedUtility.jobDefClaimGuestBed, bed) {takeExtraIngestibles = bed.rentalFee}; // Store rentalFee to avoid cheating
        }
    }
}
