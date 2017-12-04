using Verse;
using Verse.AI;

namespace Hospitality
{
    public class ThinkNode_ConditionalWantsToHelp : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            if (pawn.needs == null || pawn.needs.mood == null) return false;
            return pawn.needs.mood.CurLevel > 0.75f;
        }
    }
}