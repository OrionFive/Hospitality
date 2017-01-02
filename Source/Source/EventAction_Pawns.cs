using System.Collections.Generic;
using Verse;

namespace Hospitality
{
    public abstract class EventAction_Pawns : EventAction {
        protected List<Pawn> pawns = new List<Pawn>();

        public void RemovePawn(Pawn p)
        {
            pawns.Remove(p);
        }

        public override void ExposeData()
        {
            Scribe_Collections.LookList(ref pawns, "visitingPawns", LookMode.MapReference);
        }
    }
}