using RimWorld;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public class JobGiver_Sleep : ThinkNode
    {
        public override float GetPriority(Pawn pawn)
        {
            if (pawn.needs?.rest == null)
            {
                Log.Message(pawn.Name.ToStringShort + " needs no rest...");
                return 0f;
            }
            float curLevel = pawn.needs.rest.CurLevel;

            int hourOfDay = GenLocalDate.HourOfDay(pawn);
            if (hourOfDay < 7 || hourOfDay > 21)
            {
                curLevel -= 0.2f;
            }

            if (curLevel < 0.35f)
            {
                return 6f;
            }

            if (curLevel > 0.4f)
            {
                return 0f;
            }
            return 1-curLevel;
        }

        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            if (pawn.CurJob != null)
            {
                //Log.Message(pawn.NameStringShort + " already has a job: " + pawn.CurJob);
                return new ThinkResult(pawn.CurJob, this);
            }
            if (pawn.needs?.rest == null)
            {
                if (pawn.needs == null) Log.ErrorOnce(pawn.Name.ToStringShort + " has no needs", 453636 + pawn.thingIDNumber);
                if (pawn.needs.rest == null) Log.ErrorOnce(pawn.Name.ToStringShort + " has no rest need", 357474 + pawn.thingIDNumber);
                return ThinkResult.NoJob;
            }
            if (pawn.mindState == null)
            {
                Log.ErrorOnce(pawn.Name.ToStringShort + " has no mindstate", 23892 + pawn.thingIDNumber);
                pawn.mindState = new Pawn_MindState(pawn);
            }

            if (Find.TickManager.TicksGame - pawn.mindState.lastDisturbanceTick < 400)
            {
                Log.Message(pawn.Name.ToStringShort + " can't sleep - got disturbed");
                return ThinkResult.NoJob;
            }
            Building_GuestBed bed = pawn.FindBedFor();
            if (bed != null)
            {
                return new ThinkResult(new Job(JobDefOf.LayDown, bed), this);
            }
            IntVec3 vec = CellFinder.RandomClosewalkCellNear(pawn.mindState.duty.focus.Cell, pawn.MapHeld, 4);
            if(!pawn.CanReserve(vec)) return ThinkResult.NoJob;

            return new ThinkResult(new Job(JobDefOf.LayDown, vec), this);
        }
    }
}