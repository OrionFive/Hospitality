using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Hospitality
{
    public class Trigger_UrgentlyCold : Trigger
    {
        public override bool ActivateOn(Lord lord, TriggerSignal signal)
        {
            if (signal.type == TriggerSignalType.Tick)
            {
                return lord != null && lord.ownedPawns.Any(TooCold);
            }
            return false;
        }

        private static bool TooCold(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null) return false;
            var hypoHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Hypothermia);
            if (hypoHediff == null) return false;
            return hypoHediff.CurStageIndex > 1;
        }
    }
}