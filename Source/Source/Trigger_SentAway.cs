using Verse;
using Verse.AI.Group;

namespace Hospitality
{
    public class Trigger_SentAway : Trigger
    {
        public override bool ActivateOn(Lord lord, TriggerSignal signal)
        {
            if (signal.type == TriggerSignalType.Tick)
            {
                return lord != null && lord.ownedPawns.Any(SentAway);
            }
            return false;
        }

        private static bool SentAway(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null) return false;
            var comp = pawn.GetComp<CompGuest>();
            if (comp == null) return false;

            return comp.sentAway;
        }
    }
}