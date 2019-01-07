using RimWorld;
using System;
using HugsLib.Utils;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace Hospitality
{
    public class ITab_Pawn_Guest : ITab_Pawn_Visitor
    {
        private static readonly string txtRecruitmentPenalty = "RecruitmentPenalty";
        private static readonly string txtFactionGoodwill = "FactionGoodwill".Translate();
        private static readonly string txtHospitality = "Hospitality".Translate();
        private static readonly string txtMakeDefault = "MakeDefault".Translate();
        private static readonly string txtForceRecruit = "ForceRecruit".Translate();
        private static readonly string txtSendAway = "SendAway".Translate();
        private static readonly string txtSendAwayQuestion = "SendAwayQuestion".Translate();

        protected static readonly Vector2 buttonSize = new Vector2(120f, 30f);
        private static Listing_Standard listingStandard = new Listing_Standard();

        public ITab_Pawn_Guest()
        {
            labelKey = "TabGuest";
            tutorTag = "Guest";
            size = new Vector2(500f, 450f);
        }

        public override bool IsVisible => SelPawn.IsGuest() || SelPawn.IsTrader();

        protected override void FillTab()
        {
            Text.Font = GameFont.Small;
            Rect rect = new Rect(0f, 20f, size.x, size.y - 20).ContractedBy(10f);
            listingStandard.Begin(rect);
            {
                if (SelPawn.IsTrader())
                {
                    FillTabTrader();
                }
                else
                {
                    FillTabGuest(rect);
                }
            }
            listingStandard.End();
        }

        private void FillTabTrader()
        {
            listingStandard.Label("IsATrader".Translate().AdjustedFor(SelPawn));
        }

        private void FillTabGuest(Rect rect)
        {
            //ConceptDatabase.KnowledgeDemonstrated(ConceptDefOf.PrisonerTab, KnowledgeAmount.GuiFrame);

            var friends = SelPawn.GetFriendsInColony();
            var friendsRequired = GuestUtility.FriendsRequired(SelPawn.MapHeld) + SelPawn.GetEnemiesInColony();
            float friendPercentage = 100f*friends/friendsRequired;

            {
                var tryImprove = SelPawn.ImproveRelationship();
                var tryRecruit = SelPawn.TryRecruit();

                listingStandard.ColumnWidth = size.x - 20;

                var comp = SelPawn.GetComp<CompGuest>();
                if (comp != null)
                {
                    listingStandard.Gap();
                    LabelWithTooltip("AreaToStay".Translate(), "AreaToStayTooltip".Translate());
                    DoAreaRestriction(listingStandard, comp.GuestArea, SetAreaRestriction, AreaUtility.AreaAllowedLabel_Area);
                    LabelWithTooltip("AreaToBuy".Translate(), "AreaToBuyTooltip".Translate());


                    DoAreaRestriction(listingStandard, comp.ShoppingArea, SetAreaShopping, GetShoppingLabel);

                    CheckboxLabeled(listingStandard, "ImproveRelationship".Translate(), ref tryImprove, false, "ImproveTooltip".Translate());

                    CheckboxLabeled(listingStandard, "ShouldTryToRecruit".Translate(), ref tryRecruit, false, "TryRecruitTooltip".Translate());

                    comp.chat = tryImprove;
                    comp.recruit = tryRecruit;

                    listingStandard.Gap(50);

                    var mayForceRecruit = !SelPawn.InMentalState && comp.arrived;

                    DrawButton(() => SetAllDefaults(SelPawn), txtMakeDefault, new Vector2(rect.xMax - buttonSize.x - 10, 160));
                    DrawButton(() => SendHomeDialog(SelPawn.GetLord()), txtSendAway, new Vector2(rect.xMin - 10, 160));
                    if (mayForceRecruit)
                    {
                        DrawButton(() => ForceRecruitDialog(SelPawn), txtForceRecruit, new Vector2(rect.xMin - 10 + 10 + buttonSize.x, 160));
                    }
                }

                if (SelPawn.Faction != null)
                {
                    listingStandard.Label(txtRecruitmentPenalty.Translate(SelPawn.RecruitPenalty().ToString("##0"), SelPawn.ForcedRecruitPenalty().ToString("##0")));
                    listingStandard.Label(txtFactionGoodwill + ": " + SelPawn.Faction.PlayerGoodwill.ToString("##0"));
                }
                listingStandard.Gap();

                listingStandard.Label($"{"FriendsRequirement".Translate(friends, friendsRequired)}:");

                listingStandard.Slider(Mathf.Clamp(friendPercentage, 0, 100), 0, 100);
                if (friendPercentage <= 99)
                {
                    var color = GUI.color;
                    GUI.color = Color.red;
                    listingStandard.Label("NotEnoughFriends".Translate(SelPawn.GetMinRecruitOpinion()).AdjustedFor(SelPawn));
                    GUI.color = color;
                }
                else
                {
                    listingStandard.Label("CanNowBeRecruited".Translate().AdjustedFor(SelPawn));
                }


                // Will only have lord while "checked in", becomes null again when guests leave
                if (SelPawn.GetVisitScore(out var score))
                {
                    listingStandard.Label(txtHospitality + ":");
                    listingStandard.Slider(score, 0f, 1f);
                }
            }
        }

        private static void LabelWithTooltip(string label, string tooltip)
        {
            var rect = listingStandard.GetRect(Text.CalcHeight(label, listingStandard.ColumnWidth));
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

        private static string GetShoppingLabel(Area area)
        {
            if (area != null) return area.Label;
            return "AreaNoShopping".Translate();
        }

        private void DoAreaRestriction(Listing_Standard listing, Area area, Action<Area> setArea, Func<Area, string> getLabel)
        {
            var areaRect = listing.GetRect(24);

            // Needed for GUI
            if (SelPawn.playerSettings == null)
            {
                SelPawn.playerSettings = new Pawn_PlayerSettings(SelPawn) {AreaRestriction = area};
            }

            SelPawn.playerSettings.AreaRestriction = area;
            GuestUtility.DoAllowedAreaSelectors(areaRect, SelPawn, getLabel);
            var newArea = SelPawn.playerSettings.AreaRestriction;
            SelPawn.playerSettings.AreaRestriction = null;
            Text.Anchor = TextAnchor.UpperLeft;

            if (newArea != area) setArea(newArea);
        }

        private void SetAreaRestriction(Area area)
        {
            foreach (var pawn in SelPawn.GetLord().ownedPawns)
            {
                pawn.GetComp<CompGuest>().GuestArea = area;
            }
        }

        private void SetAreaShopping(Area area)
        {
            foreach (var pawn in SelPawn.GetLord().ownedPawns)
            {
                pawn.GetComp<CompGuest>().ShoppingArea = area;
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

        private static void DrawButton(Action action, string text, Vector2 pos)
        {
            var rect = new Rect(pos.x, pos.y, buttonSize.x, buttonSize.y);
            if (Widgets.ButtonText(rect, text))
            {
                SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera();

                action();
            }
        }

        private static void ForceRecruitDialog(Pawn pawn)
        {
            var forcedPenalty = pawn.ForcedRecruitPenalty();
            int finalGoodwill = Mathf.Clamp(pawn.Faction.PlayerGoodwill - forcedPenalty, -100, 100);

            string warning = finalGoodwill <= DiplomacyTuning.BecomeHostileThreshold ? "ForceRecruitWarning".Translate() : string.Empty;

            var text = "ForceRecruitQuestion".Translate(forcedPenalty, warning, new NamedArgument(pawn, "PAWN"));
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, () => { GuestUtility.ForceRecruit(pawn, forcedPenalty); }));
        }

        private static void SendHomeDialog(Lord lord)
        {
            var text = string.Format(txtSendAwayQuestion, lord.faction.Name);
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, () => SendHome(lord)));
        }

        private static void SendHome(Lord lord)
        {
            foreach (var pawn in lord.ownedPawns)
            {
                pawn.GetComp<CompGuest>().sentAway = true;
            }
        }

        private void SetAllDefaults(Pawn pawn)
        {
            Map map = SelPawn.MapHeld;
            if (map == null) return;

            var mapComp = Hospitality_MapComponent.Instance(map);

            if (pawn.GetComp<CompGuest>() != null)
            {
                mapComp.defaultInteractionMode = pawn.GetComp<CompGuest>().chat ? PrisonerInteractionModeDefOf.ReduceResistance : PrisonerInteractionModeDefOf.NoInteraction;
            }
            
            mapComp.defaultAreaRestriction = pawn.GetGuestArea();
            mapComp.defaultAreaShopping = pawn.GetShoppingArea();

            var guests = GuestUtility.GetAllGuests(map);
            foreach (var guest in guests)
            {
                var comp = guest.GetComp<CompGuest>();
                if (comp != null)
                {
                    comp.chat = mapComp.defaultInteractionMode == PrisonerInteractionModeDefOf.ReduceResistance;
                    comp.GuestArea = mapComp.defaultAreaRestriction;
                    comp.ShoppingArea = mapComp.defaultAreaShopping;
                }
            }
        }
    }
}
