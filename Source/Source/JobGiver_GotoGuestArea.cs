using RimWorld;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public class JobGiver_GotoGuestArea : ThinkNode
    {
        public override float GetPriority(Pawn pawn)
        {
            var area = pawn.GetGuestArea();
            if (area == null) return 0;
            if (area.TrueCount == 0) return 0;

            return area[pawn.PositionHeld] ? 0 : 10;

        }

        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            var area = pawn.GetGuestArea();
            if (area == null) return ThinkResult.NoJob;
            if(area.TrueCount == 0) return ThinkResult.NoJob;

            IntVec3 closeSpot;
            CellFinder.TryFindRandomReachableCellNear(area.ActiveCells.RandomElement(), pawn.MapHeld, 20,
                TraverseParms.For(pawn, Danger.Some, TraverseMode.PassDoors), c=>area[c], null, out closeSpot);

            return new ThinkResult(new Job(JobDefOf.Goto, closeSpot){locomotionUrgency = LocomotionUrgency.Jog}, this);
        }
    }
}