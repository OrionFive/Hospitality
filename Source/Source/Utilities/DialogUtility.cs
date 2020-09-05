using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Hospitality
{
    internal static class DialogUtility
    {
        private static readonly string txtForceRecruit = "ForceRecruit".Translate();
        private static readonly string txtRecruit = "Recruit".Translate();
        private static readonly string txtCantRecruit = "CantRecruit".Translate();
        private static readonly string txtCantForce = "CantForceRecruit".Translate();
        private static readonly string txtRecruitTooltip = "RecruitTooltip".Translate();
        private static readonly string txtForceRecruitTooltip = "ForceRecruitTooltip".Translate();


        public static void LabelWithTooltip(string label, string tooltip, Rect rect)
        {
            Widgets.Label(rect, label);
            DoTooltip(rect, tooltip);
        }

        private static void DoTooltip(Rect rect, string tooltip)
        {
            if (!tooltip.NullOrEmpty())
            {
                if (Mouse.IsOver(rect)) Widgets.DrawHighlight(rect);
                TooltipHandler.TipRegion(rect, tooltip);
            }
        }

        public static void CheckboxLabeled(Listing_Standard listing, string label, ref bool checkOn, Rect rect, bool disabled = false, string tooltip = null)
        {
            if (!tooltip.NullOrEmpty())
            {
                if (Mouse.IsOver(rect))
                    Widgets.DrawHighlight(rect);
                TooltipHandler.TipRegion(rect, tooltip);
            }

            Widgets.CheckboxLabeled(rect, label, ref checkOn, disabled);
            listing.Gap(listing.verticalSpacing);
        }

        public static void DrawRecruitButton(Rect rect, bool hasEnoughFriends, bool isRoyal, Pawn pawn)
        {
            var disabled = !pawn.MayRecruitAtAll() || !pawn.MayRecruitRightNow();
            if (hasEnoughFriends)
            {
                if (disabled) DrawButtonDisabled(txtRecruit, rect, txtCantRecruit);
                else DrawButton(() => ITab_Pawn_Guest.RecruitDialog(pawn, false), txtRecruit, rect, txtRecruitTooltip);
            }
            else if (!isRoyal)
            {
                if (disabled) DrawButtonDisabled(txtRecruit, rect, txtCantRecruit);
                else DrawButton(() => ITab_Pawn_Guest.RecruitDialog(pawn, true), txtForceRecruit, rect, txtForceRecruitTooltip);
            }
            else
            {
                DrawButtonDisabled(txtRecruit, rect, disabled ? txtCantRecruit : txtCantForce);
            }
        }

        public static void DrawButton(Action action, string text, Rect rect, string tooltip = null)
        {
            if (!tooltip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }

            if (Widgets.ButtonText(rect, text, true, true, Widgets.NormalOptionColor))
            {
                SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera();

                action();
            }
        }

        private static void DrawButtonDisabled(string text, Rect rect, string textDenied = null, string tooltip = null)
        {
            if (!tooltip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }

            GUI.color = Color.gray;
            if(Widgets.ButtonText(rect, text, true, false, Widgets.NormalOptionColor))
            {
                if (!textDenied.NullOrEmpty())
                {
                    Messages.Message(textDenied, MessageTypeDefOf.RejectInput);
                }
            }
            GUI.color = Color.white;
        }
    }
}
