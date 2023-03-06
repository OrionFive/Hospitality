using System.Linq;
using UnityEngine;
using Verse;

namespace Hospitality
{
    public class Gizmo_VendingMachineContent : Gizmo_ModifyNumber<CompVendingMachine>
    {
        private readonly CompVendingMachine vendingMachine;

        public Gizmo_VendingMachineContent(CompVendingMachine[] vendingMachines) : base(vendingMachines)
        {
            vendingMachine = vendingMachines.First();
        }

        public override bool GroupsWith(Gizmo other) => false;

        protected override Color ButtonColor { get; } = new Color(249 / 256f, 178 / 256f, 86 / 256f);

        protected override string Title => "Hospitality_VendingMachineContent".Translate();

        protected override void ButtonDown() => vendingMachine.CurrentEmptyThreshold -= vendingMachine.Properties.priceSteps;

        protected override void ButtonUp() => vendingMachine.CurrentEmptyThreshold += vendingMachine.Properties.priceSteps;

        protected override void ButtonCenter() => vendingMachine.CurrentEmptyThreshold = vendingMachine.CurrentPrice * 10;

        protected override void DrawInfoRect(Rect rect)
        {
            LabelRow(ref rect, "Hospitality_VendingMachineThreshold".Translate(), ((float)vendingMachine.CurrentEmptyThreshold).ToStringMoney("F0"));
            GUI.color = ButtonColor;
            LabelRow(ref rect, "Hospitality_VendingMachineContains".Translate(), ((float)vendingMachine.TotalSold).ToStringMoney("F0"));
            GUI.color = Color.white;
        }

        protected override void DrawTooltipBox(Rect totalRect) { }
    }
}
