using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace Hospitality
{
    public class ITab_Pawn_Guest : ITab_Pawn_Visitor
    {
        private static readonly string txtRecruitmentChance = "RecruitmentChance".Translate();
        private static readonly string txtRecruitmentPenalty = "RecruitmentPenalty".Translate();
        private static readonly string txtFactionGoodwill = "FactionGoodwill".Translate();
        private static readonly string txtHospitality = "Hospitality".Translate();
        private static readonly string txtSetDefault = "SetDefault".Translate();

        protected readonly Vector2 setDefaultButtonSize = new Vector2(120f, 30f);
       
        public ITab_Pawn_Guest()
        {
            labelKey = "TabGuest";
            size = new Vector2(400f, 380f);
        }

        public override bool IsVisible { get { return SelPawn.IsGuest(); } }

        protected override void FillTab()
        {
            //ConceptDatabase.KnowledgeDemonstrated(ConceptDefOf.PrisonerTab, KnowledgeAmount.GuiFrame);
            Text.Font = GameFont.Small;
            Rect rect1 = new Rect(0f, 20f, size.x, size.y - 20).ContractedBy(10f);
            var listingStandard = new Listing_Standard(rect1);

            var trust = SelPawn.RelativeTrust();

            {
                var tryImprove = SelPawn.ImproveRelationship();
                var tryRecruit = SelPawn.TryRecruit();

                listingStandard.ColumnWidth = size.x - 20;

                var comp = SelPawn.GetComp<CompGuest>();
                if (comp != null)
                {
                    listingStandard.Gap();

                    CheckboxLabeled(listingStandard, "ImproveRelationship".Translate(), ref tryImprove);
                    CheckboxLabeled(listingStandard, "ShouldTryToRecruit".Translate(), ref tryRecruit);

                    comp.chat = tryImprove;
                    comp.recruit = tryRecruit;

                    listingStandard.Gap(50);

                    DrawSetDefaultButton(rect1);
                }

                if (SelPawn.Faction != null)
                {
                    listingStandard.Label(txtRecruitmentPenalty + ": " + SelPawn.RecruitPenalty().ToString("##0"));
                    listingStandard.Label(txtFactionGoodwill + ": " + SelPawn.Faction.PlayerGoodwill.ToString("##0"));
                }
                listingStandard.Gap();

                // Will only have squadBrain while "checked in", becomes null again when guests leave
                var squadBrain = SelPawn.GetLord();
                if (squadBrain != null)
                {
                    listingStandard.Label(string.Format("{0}:", txtRecruitmentChance));
                    listingStandard.Slider(Mathf.Clamp(trust, 0, 100), 0, 100);
                    if (trust < 50)
                    {
                        var color = GUI.color;
                        GUI.color = Color.red;
                        listingStandard.Label("TrustTooLow".Translate().AdjustedFor(SelPawn));
                        GUI.color = color;
                    }

                    var lordToil = squadBrain.CurLordToil as LordToil_VisitPoint;
                    if (lordToil != null && SelPawn.Faction != null)
                    {
                        listingStandard.Label(txtHospitality + ":");
                        listingStandard.Slider(lordToil.GetVisitScore(SelPawn), 0f, 1f);
                    }
                }
            }
            listingStandard.End();
        }

        public void CheckboxLabeled(Listing_Standard listing, string label, ref bool checkOn, bool disabled = false, string tooltip = null)
        {
            Rect rect = listing.GetRect(Text.LineHeight);
            if (!tooltip.NullOrEmpty())
            {
                if (Mouse.IsOver(rect))
                    Widgets.DrawHighlight(rect);
                TooltipHandler.TipRegion(rect, tooltip);
            }
            Widgets.CheckboxLabeled(rect, label, ref checkOn, disabled);
            listing.Gap(listing.verticalSpacing);
        }


        private void DrawSetDefaultButton(Rect rect1)
        {
            Rect rect2 = new Rect(rect1.xMax - setDefaultButtonSize.x - 10f, 70f, setDefaultButtonSize.x, setDefaultButtonSize.y);
            if (Widgets.ButtonText(rect2, txtSetDefault))
            {
                var list = new List<FloatMenuOption>
                {
                    new FloatMenuOption("LeaveAlone".Translate(),
                        () => SetDefaults(PrisonerInteractionMode.NoInteraction), 0),

                    new FloatMenuOption("ImproveRelationship".Translate(),
                        () => SetDefaults(PrisonerInteractionMode.Chat), 0)
                };

                Find.WindowStack.Add(new FloatMenu(list));
            }
        }

        private void SetDefaults(PrisonerInteractionMode mode)
        {
            Map map = SelPawn.MapHeld;
            if (map == null) return;

            var oldMode = Hospitality_MapComponent.Instance(map).defaultInteractionMode;
            if (oldMode == mode) return;

            Hospitality_MapComponent.Instance(map).defaultInteractionMode = mode;

            var guests = GuestUtility.GetAllGuests(map);
            foreach (var guest in guests)
            {
                var comp = guest.GetComp<CompGuest>();
                if (comp == null) continue;
                comp.chat = mode == PrisonerInteractionMode.Chat;
            }
        }
    }
}
