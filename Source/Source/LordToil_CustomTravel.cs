using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Hospitality
{
    public class LordToilData_CustomTravel : LordToilData
    {
        public IntVec3 dest;
        public bool destAssigned;
        public float percentRequired;

        public override void ExposeData()
        {
            Scribe_Values.LookValue(ref dest, "dest", default(IntVec3));
            Scribe_Values.LookValue(ref destAssigned, "destAssigned", false);
            Scribe_Values.LookValue(ref percentRequired, "percentRequired", 1);
        }
    }

    public class LordToil_CustomTravel : LordToil
    {
        private LordToilData_CustomTravel Data { get { return data as LordToilData_CustomTravel; } }
        public override IntVec3 FlagLoc { get { return Data.dest; } }

        public LordToil_CustomTravel() {}

        public LordToil_CustomTravel(IntVec3 dest, float percentRequired = 1)
        {
            data = new LordToilData_CustomTravel {dest = dest, destAssigned = true, percentRequired = percentRequired};
        }

        public override void UpdateAllDuties()
        {
            foreach (Pawn pawn in lord.ownedPawns) pawn.mindState.duty = new PawnDuty(DutyDefOf.Travel, Data.dest, -1f);
        }

        public override void Init()
        {
            base.Init();
            if (Data.destAssigned) return;
            if (!RCellFinder.TryFindTravelDestFrom(lord.ownedPawns[0].Position, out Data.dest))
            {
                Log.Error("Travelers for " + lord.faction + " could not late-find travel destination.");
                Data.dest = lord.ownedPawns[0].Position;
            }
            Data.destAssigned = true;
        }

        public override void LordToilTick()
        {
            if (Find.TickManager.TicksGame%205 != 0) return;
            int count = lord.ownedPawns.Count(pawn => pawn != null && pawn.Position.InHorDistOf(Data.dest, 10f) && pawn.CanReach(Data.dest, PathEndMode.OnCell, Danger.Some));
            float percent = 1f*count/lord.ownedPawns.Count(pawn => pawn != null);
            if (Data == null) return;
            if (percent < Data.percentRequired) return;
            lord.ReceiveMemo("TravelArrived");
        }
    }
}
