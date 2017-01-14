using System.Collections.Generic;
using CommunityCoreLibrary;
using Hospitality.Detouring;
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
        private static readonly string txtMakeDefault = "MakeDefault".Translate();

        protected readonly Vector2 setDefaultButtonSize = new Vector2(120f, 30f);
       
        public ITab_Pawn_Guest()
        {
            labelKey = "TabGuest";
            size = new Vector2(400f, 380f);
        }

        public override bool IsVisible { get { return SelPawn.IsGuest() || SelPawn.IsTrader(); } }

        protected override void FillTab()
        {
            Text.Font = GameFont.Small;
            Rect rect = new Rect(0f, 20f, size.x, size.y - 20).ContractedBy(10f);
            var listingStandard = new Listing_Standard(rect);
            {
                if (SelPawn.IsTrader())
                {
                    FillTabTrader(listingStandard);
                }
                else
                {
                    FillTabGuest(listingStandard, rect);
                }
            }
            listingStandard.End();
        }

        private void FillTabTrader(Listing_Standard listingStandard)
        {
            listingStandard.Label("IsATrader".Translate().AdjustedFor(SelPawn));
        }

        private void FillTabGuest(Listing_Standard listingStandard, Rect rect)
        {
            //ConceptDatabase.KnowledgeDemonstrated(ConceptDefOf.PrisonerTab, KnowledgeAmount.GuiFrame);

            var trust = SelPawn.RelativeTrust();

            {
                var tryImprove = SelPawn.ImproveRelationship();
                var tryRecruit = SelPawn.TryRecruit();

                listingStandard.ColumnWidth = size.x - 20;

                var comp = SelPawn.GetComp<CompGuest>();
                if (comp != null)
                {
                    listingStandard.Gap();
                    
                    DoAreaRestriction(listingStandard, comp);

                    CheckboxLabeled(listingStandard, "ImproveRelationship".Translate(), ref tryImprove);
                    CheckboxLabeled(listingStandard, "ShouldTryToRecruit".Translate(), ref tryRecruit);

                    comp.chat = tryImprove;
                    comp.recruit = tryRecruit;

                    listingStandard.Gap(50);

                    DrawSetDefaultButton(rect);
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
        }

        private void DoAreaRestriction(Listing_Standard listingStandard, CompGuest comp)
        {
            var areaRect = listingStandard.GetRect(24);
            if (SelPawn.playerSettings == null)
            {
                var savedArea = comp.GuestArea;
                SelPawn.playerSettings = new RimWorld.Pawn_PlayerSettings(SelPawn) {AreaRestriction = savedArea};
            }

            var oldArea = SelPawn.playerSettings.AreaRestriction = comp.GuestArea;
            AreaAllowedGUI.DoAllowedAreaSelectors(areaRect, SelPawn, AllowedAreaMode.Humanlike);
            Text.Anchor = TextAnchor.UpperLeft;

            if (SelPawn.playerSettings.AreaRestriction != oldArea) SetAreaRestriction(SelPawn.GetLord(), SelPawn.playerSettings.AreaRestriction);
        }

        private static void SetAreaRestriction(Lord lord, Area areaRestriction)
        {
            foreach (var pawn in lord.ownedPawns)
            {
                pawn.GetComp<CompGuest>().GuestArea = areaRestriction;
            }
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
            Rect rect2 = new Rect(rect1.xMax - setDefaultButtonSize.x - 10f, 90f, setDefaultButtonSize.x, setDefaultButtonSize.y);
            if (Widgets.ButtonText(rect2, txtMakeDefault))
            {
                SoundDefOf.DesignateDragStandardChanged.PlayOneShotOnCamera();

                SetAllDefaults(SelPawn);
                //var list = new List<FloatMenuOption>
                //{
                //    new FloatMenuOption("LeaveAlone".Translate(),
                //        () => SetDefaults(PrisonerInteractionMode.NoInteraction), 0),
                //
                //    new FloatMenuOption("ImproveRelationship".Translate(),
                //        () => SetDefaults(PrisonerInteractionMode.Chat), 0)
                //};
                //
                //Find.WindowStack.Add(new FloatMenu(list));
            }
        }

        private void SetAllDefaults(Pawn pawn)
        {
            Map map = SelPawn.MapHeld;
            if (map == null) return;

            var mapComp = Hospitality_MapComponent.Instance(map);

            if(pawn.GetComp<CompGuest>() != null)
            {
                mapComp.defaultInteractionMode = pawn.GetComp<CompGuest>().chat
                ? PrisonerInteractionMode.Chat
                : PrisonerInteractionMode.NoInteraction;
            }

            if (pawn.playerSettings != null)
            {
                mapComp.defaultAreaRestriction = pawn.GetComp<CompGuest>().GuestArea;
            }

            var guests = GuestUtility.GetAllGuests(map);
            foreach (var guest in guests)
            {
                var comp = guest.GetComp<CompGuest>();
                if (comp != null)
                {
                    comp.chat = mapComp.defaultInteractionMode == PrisonerInteractionMode.Chat;
                    comp.GuestArea = mapComp.defaultAreaRestriction;
                }
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
