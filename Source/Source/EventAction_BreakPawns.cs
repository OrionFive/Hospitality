using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace Hospitality
{
    public class EventAction_BreakPawns : EventAction_Pawns
    {
        public EventAction_BreakPawns(List<Pawn> pawns, PawnLostCondition condition)
        {
            Log.Message("Break reason: "+condition);
            this.pawns = pawns;
        }
        
        public EventAction_BreakPawns()
        {
            //
        }

        public override void DoAction()
        {
            foreach (var pawn in pawns.Where(p => p != null))
            {
                pawn.Break();
            }
        }
    }
}