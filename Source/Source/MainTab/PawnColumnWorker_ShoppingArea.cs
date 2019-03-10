using RimWorld;
using UnityEngine;
using Verse;

namespace Hospitality.MainTab
{
    class PawnColumnWorker_ShoppingArea : PawnColumnWorker_AreaBase
    {
        protected override Area GetArea(Pawn pawn)
        {
            var comp = pawn.GetComp<CompGuest>();
            return comp?.ShoppingArea;
        }

        protected override void SetArea(Pawn pawn, Area area)
        {
            var comp = pawn.GetComp<CompGuest>();
            if (comp != null) comp.ShoppingArea = area;
        }

        protected override void DrawTopArea(Rect rect2)
        {
            // Don't draw Manage Areas button
            //if (Widgets.ButtonText(rect2, "ManageAreas".Translate(), true, false, true))
            //{
            //    Find.WindowStack.Add(new Dialog_ManageAreas(Find.CurrentMap));
            //}
        }

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            // Changed check
            if (!pawn.IsGuest()) return;

            GenericUtility.DoAreaRestriction(pawn, rect, GetArea(pawn), area=>SetArea(pawn, area), GenericUtility.GetShoppingLabel);
        }
    }
}
