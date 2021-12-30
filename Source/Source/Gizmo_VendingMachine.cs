using UnityEngine;
using Verse;

namespace Hospitality
{
    public class Gizmo_VendingMachine : Gizmo_ModifyNumber
    {
        internal CompVendingMachine vendingMachine;
        protected override Color ButtonColor { get; } = new Color(249 / 256f, 178 / 256f, 86 / 256f);

        protected override string Title => "Hospitality_VendingMachine".Translate();

        protected override void ButtonDown() => vendingMachine.CurrentPrice -= 5;

        protected override void ButtonUp() => vendingMachine.CurrentPrice += 5;

        protected override void ButtonCenter() => vendingMachine.SetAutoPricing();

        protected override void DrawInfoRect(Rect rect)
        {
            LabelRow(ref rect, "Hospitality_VendingMachinePrice".Translate(), ((float)vendingMachine.CurrentPrice).ToStringMoney());
            GUI.color = ButtonColor;
            LabelRow(ref rect, "Hospitality_VendingMachineContains".Translate(), vendingMachine.TotalSold);
            GUI.color = Color.white;
        }
    }
}
