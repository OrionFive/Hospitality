using Verse;
using Verse.AI;

namespace Hospitality {
    public class ThinkNode_CheckForNoLord : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            var compGuest = pawn.GetComp<CompGuest>();
            //Log.Message($"{pawn.LabelShort}: Has no lord: {compGuest?.lord == null}");
            return compGuest?.lord == null;
        }
    }
}