using RimWorld;
using Verse;

namespace Hospitality.MainTab
{
    public class PawnColumnWorker_Entertain : PawnColumnWorker_Checkbox
    {
        public override bool HasCheckbox(Pawn pawn) => pawn.IsGuest();

        public override bool GetValue(Pawn pawn) => pawn.ImproveRelationship();

        public override void SetValue(Pawn pawn, bool value)
        {
            var compGuest = pawn.CompGuest();
            if (compGuest != null) compGuest.entertain = value;
        }
    }
}
