using Hospitality.Utilities;
using UnityEngine;
using Verse;
using Text = Verse.Text;

namespace Hospitality
{
    public class Gizmo_VendingMachine : Gizmo
    {
        internal CompVendingMachine vendingMachine;
         
        public Gizmo_VendingMachine()
        {
            order = -25f;
        }

        public override float GetWidth(float maxWidth) => 200;

        private const float MainRectWidth = 175;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect TotalRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Widgets.DrawWindowBackground(TotalRect);

            var MainRect = TotalRect.LeftPartPixels(MainRectWidth).ContractedBy(5);

            //Begin Group For Price Labels
            GUI.BeginGroup(MainRect);
            Rect rect = new Rect(0, 0, MainRect.width, MainRect.height);
            float curY = 0;

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperCenter;
            string label = "Hospitality_VendingMachine".Translate();
            var size = Text.CalcSize(label);
            Rect labelRect = new Rect(0, curY, rect.width, size.y);
            curY += size.y;
            Widgets.Label(labelRect, label);

            //Draw Selling Config
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            Rect priceLabelRect = new Rect(0, curY, MainRect.width, 20);
            curY += priceLabelRect.height + 1;
            Rect earningLabelRect = new Rect(0, curY, MainRect.width, 20);

            LabelRow(priceLabelRect,75,$"${vendingMachine.CurrentPrice}", "Hospitality_VendingMachinePrice".Translate(), GameFont.Small);
            GUI.color = Color.green;
            LabelRow(earningLabelRect,75, vendingMachine.TotalSold, "Hospitality_VendingMachineContains".Translate(), GameFont.Tiny);
            GUI.color = Color.white;

            Text.Anchor = default;
            Text.Font = default;

            GUI.EndGroup();

            Rect SettingsRect = TotalRect.RightPartPixels(35).ContractedBy(5);
            var settingSize = SettingsRect.width;
            var halfSize = SettingsRect.height / 2f;
            Rect middleSetting = new Rect(SettingsRect.x, (SettingsRect.y + halfSize) - (settingSize / 4), settingSize, settingSize / 2);

            Rect upperSetting = new Rect(SettingsRect.x, middleSetting.y - settingSize, settingSize, settingSize);
            Rect lowerSettings = new Rect(SettingsRect.x, middleSetting.yMax, settingSize, settingSize);

            if (Widgets.ButtonImage(middleSetting, HospitalityContent.VendingPriceAuto))
            {
                vendingMachine.SetAutoPricing();
            }
            if (Widgets.ButtonImage(upperSetting, HospitalityContent.VendingPriceUp))
            {
                vendingMachine.CurrentPrice += 5;
            }
            if (Widgets.ButtonImage(lowerSettings, HospitalityContent.VendingPriceDown))
            {
                vendingMachine.CurrentPrice -= 5;
            }

            return new GizmoResult(GizmoState.Mouseover);
        }

        private static void LabelRow(Rect inRect, int labelX, string label, string title, GameFont font = GameFont.Tiny)
        {
            Text.Font = font;
            var titleSize = Text.CalcSize(title);
            var labelSize = Text.CalcSize(label);

            Rect titleRect = new Rect(inRect.x, inRect.y, titleSize.x, titleSize.y);
            Rect labelRect = new Rect(inRect.x + labelX, inRect.y, labelSize.x, labelSize.y);

            Widgets.Label(titleRect, title);
            Widgets.Label(labelRect, label);

            Text.Font = default;
        }

        private void LabelWithTitle(Rect inRect, string label, string title)
        {
            Text.Font = GameFont.Tiny;

            var titleSize = Text.CalcSize(title);
            Rect titleRect = inRect.TopPartPixels(titleSize.y);
            Widgets.Label(titleRect, title);

            Text.Font = GameFont.Small;

            Rect labelRect = inRect.BottomPartPixels(inRect.height - titleSize.y);
            Widgets.Label(labelRect, label);
        }
    }
}
