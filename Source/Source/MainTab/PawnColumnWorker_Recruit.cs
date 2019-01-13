using RimWorld;
using Verse;

namespace Hospitality.MainTab
{
    public class PawnColumnWorker_Recruit : PawnColumnWorker_Checkbox
    {
        protected override bool HasCheckbox(Pawn pawn) => pawn.IsGuest();

        protected override bool GetValue(Pawn pawn) => pawn.TryRecruit();

        protected override void SetValue(Pawn pawn, bool value)
        {
            var compGuest = pawn.GetComp<CompGuest>();
            if (compGuest != null) compGuest.recruit = value;
        }
    }
}
