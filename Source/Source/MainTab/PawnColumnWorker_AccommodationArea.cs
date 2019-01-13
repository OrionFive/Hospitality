using RimWorld;
using UnityEngine;
using Verse;

namespace Hospitality.MainTab
{
    class PawnColumnWorker_AccommodationArea : PawnColumnWorker_AreaBase
    {
        protected override Area GetArea(Pawn pawn)
        {
            var comp = pawn.GetComp<CompGuest>();
            return comp?.GuestArea;
        }

        protected override void SetArea(Pawn pawn, Area area)
        {
            var comp = pawn.GetComp<CompGuest>();
            if (comp != null) comp.GuestArea = area;
        }

        protected override void DrawTopArea(Rect rect2)
        {
            if (Widgets.ButtonText(rect2, "ManageAreas".Translate(), true, false, true))
            {
                Find.WindowStack.Add(new Dialog_ManageAreas(Find.CurrentMap));
            }
        }
    }
}
