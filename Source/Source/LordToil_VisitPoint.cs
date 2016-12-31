using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Hospitality
{
    internal class LordToilData_VisitPoint : LordToilData
    {
        public IntVec3 point;
        public float radius;
        public Dictionary<int, float> visitorMoods = new Dictionary<int, float>();
        public VisitorFlag visitorFlag;

        public override void ExposeData()
        {
            Scribe_Values.LookValue(ref point, "point", default(IntVec3));
            Scribe_Values.LookValue(ref radius, "radius", 45f);
            Scribe_Collections.LookDictionary(ref visitorMoods, "visitorMoods");
            Scribe_References.LookReference(ref visitorFlag, "flag");
        }
    }
    internal class LordToil_VisitPoint : LordToil
    {
        public override IntVec3 FlagLoc
        {
            get { return Data.point; }
        }
        public LordToilData_VisitPoint Data { get { return (LordToilData_VisitPoint) data; } }

        public LordToil_VisitPoint(IntVec3 point)
        {
            data = new LordToilData_VisitPoint {point = point};
        }

        public override void Init()
        {
            base.Init();
            //Log.Message("Init State_VisitPoint "+brain.ownedPawns.Count + " - "+brain.faction.name);
            foreach (var pawn in lord.ownedPawns)
            {
                if (pawn.needs == null || pawn.needs.mood == null) Data.visitorMoods.Add(pawn.thingIDNumber, 0.5f);
                else
                    Data.visitorMoods.Add(pawn.thingIDNumber, pawn.needs.mood.CurLevel);
                //Log.Message("Added "+pawn.NameStringShort+": "+pawn.needs.mood.CurLevel);

                var newColony = -0.1f; // Mathf.Lerp(-0.15f, -0.05f, GenDate.MonthsPassed/20f); // bonus for new colony
                var regularity = Mathf.Lerp(-0.5f, 0.25f, Mathf.InverseLerp(-100, 100, lord.faction.PlayerGoodwill)); // negative factions have lower expectations
                float expectations = newColony + regularity;
                Data.visitorMoods[pawn.thingIDNumber] += expectations;


                pawn.PocketHeadgear();
            }

            PlaceFlag();
        }

        private void PlaceFlag()
        {
            var def = ThingDef.Named("VisitorFlag");
            Data.visitorFlag = (VisitorFlag)GenSpawn.Spawn(def, FlagLoc);
            //Data.visitorFlag.SetFaction(lord.faction);
            var pawn = lord.ownedPawns.FirstOrDefault();
            Data.visitorFlag.SetLord(lord);
            if (pawn == null) return;
            
            Data.visitorFlag.SetColor(PawnNameColorUtility.PawnNameColorOf(pawn));

            // Lessons
            LessonAutoActivator.TeachOpportunity(ConceptDef.Named("GuestBeds"), Data.visitorFlag, OpportunityType.Important);
            if (PlayerHasSkilledNegotiator)
            {
                LessonAutoActivator.TeachOpportunity(ConceptDef.Named("RecruitGuest"), Data.visitorFlag, OpportunityType.GoodToKnow);
            }
        }

        public bool PlayerHasSkilledNegotiator
        {
            get
            {
                if (Find.MapPawns.FreeColonistsSpawnedCount == 0) return false;
                return Find.MapPawns.FreeColonistsSpawned.Any(
                    p => p != null && !p.Dead
                        && p.skills.AverageOfRelevantSkillsFor(DefDatabase<WorkTypeDef>.GetNamed("Warden")) >= 9);
            }
        }

        private void RemoveFlag()
        {
            if (Data.visitorFlag != null)
                Data.visitorFlag.Destroy();
            Data.visitorFlag = null;
        }

        public override void Cleanup()
        {
            Leave();

            base.Cleanup();
        }

        private void Leave()
        {
            var pawns = lord.ownedPawns.ToArray(); // Copy, because recruiting changes brain

            RemoveFlag();
            foreach (var pawn in pawns)
            {
                //if (!success)
                {
                    pawn.WearHeadgear();

                    var score = GetVisitScore(pawn);
                    if (score > 0.99f) LeaveVerySatisfied(pawn, score);
                    else if (score > 0.65f) LeaveSatisfied(pawn, score);
                }
                pawn.needs.AddOrRemoveNeedsAsAppropriate();
                //Find.Reservations.ReleaseAllClaimedBy(pawn);
                var allReservedThings = Find.Reservations.AllReservedThings().ToArray();
                foreach (var t in allReservedThings)
                {
                    if (Find.Reservations.ReservedBy(t, pawn)) Find.Reservations.Release(t, pawn);
                }
            }

            var avgScore = lord.ownedPawns.Count > 0 ? lord.ownedPawns.Average(pawn => GetVisitScore(pawn)) : 0;
            DisplayLeaveMessage(avgScore, lord.faction, lord.ownedPawns.Count);
        }

        private static void DisplayLeaveMessage(float score, Faction faction, int visitorCount)
        {
            //int goodwillChange = (int) Mathf.Clamp((score-0.5f)*20*visitorCount,-30, 30);
            float targetGoodwill = Mathf.Lerp(-100, 100, score);
            float goodwillChangeMax = Mathf.Lerp(10, 45, Mathf.InverseLerp(1, 8, visitorCount));
            float currentGoodwill = faction.GoodwillWith(Faction.OfPlayer);
            float offset = targetGoodwill - currentGoodwill;
            int goodwillChange = Mathf.RoundToInt(Mathf.Clamp(offset, -goodwillChangeMax, goodwillChangeMax));
            
            faction.AffectGoodwillWith(Faction.OfPlayer, goodwillChange);
            //float newGoodwill = faction.GoodwillWith(Faction.OfPlayer);

            if (offset >= 15)
                Messages.Message("VisitorsLeavingGreat".Translate(faction.Name, goodwillChange.ToStringWithSign()), MessageSound.Benefit);
            else if (offset >= 5)
                Messages.Message("VisitorsLeavingGood".Translate(faction.Name, goodwillChange.ToStringWithSign()), MessageSound.Benefit);
            else if (offset <= -15)
                Messages.Message("VisitorsLeavingAwful".Translate(faction.Name, goodwillChange.ToStringWithSign()), MessageSound.Negative);
            else if (offset <= -5)
                Messages.Message("VisitorsLeavingBad".Translate(faction.Name, goodwillChange.ToStringWithSign()), MessageSound.Negative);
            else
                Messages.Message("VisitorsLeavingNormal".Translate(faction.Name, goodwillChange.ToStringWithSign()), MessageSound.Standard);
        }

        public float GetVisitScore(Pawn pawn)
        {
            if (pawn.needs == null || pawn.needs.mood == null) return 0;
            var increase = pawn.needs.mood.CurLevel - Data.visitorMoods[pawn.thingIDNumber];
            var score = Mathf.Lerp(increase * 2.5f, pawn.needs.mood.CurLevel * 1.25f, 0.5f);
            //Log.Message(pawn.NameStringShort + " increase: " + (increase*2.5f) +" mood: "+(pawn.needs.mood.CurLevel*1.25f) + " score: "+score);
            return score;
        }

        private static List<Thing> GetLoot(Pawn pawn, float desiredValue)
        {
            var totalValue = 0f;
            var items = pawn.inventory.container.Where(i => WillDrop(pawn, i)).InRandomOrder().ToList();
            var dropped = new List<Thing>();
            while (totalValue < desiredValue && items.Count > 0)
            {
                var item = items.First();
                items.Remove(item);
                if (totalValue + item.MarketValue > desiredValue) continue;
                if (pawn.inventory.container.TryDrop(item, pawn.Position, ThingPlaceMode.Near, out item))
                {
                    dropped.Add(item);
                    totalValue += item.MarketValue;
                }

                // Handle trade stuff
                var twc = item as ThingWithComps;
                if (twc != null && Find.MapPawns.FreeColonistsSpawnedCount > 0) twc.PreTraded(TradeAction.PlayerBuys, Find.MapPawns.FreeColonistsSpawned.RandomElement(), pawn);
            }
            return dropped;
        }

        private static void LeaveVerySatisfied(Pawn pawn, float score)
        {
            if (pawn.inventory.container.Count == 0) return;

            var dropped = GetLoot(pawn, (score + 10)*1.5f);
            if (dropped.Count == 0) return;
            var itemNames = GenText.ToCommaList(dropped.Select(GetItemName));
            
            var text = "VisitorVerySatisfied".Translate(pawn.Name.ToStringShort, pawn.Possessive(), pawn.ProSubjCap(), itemNames);
            Messages.Message(text, dropped.First(), MessageSound.Benefit);

            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDef.Named("Hospitality"), KnowledgeAmount.Total);
        }

        private static void LeaveSatisfied(Pawn pawn, float score)
        {
            if (pawn.inventory.container.Count == 0) return;

            var desiredValue = (score + 10)*2;
            var things = pawn.inventory.container.Where(i => WillDrop(pawn, i) && i.MarketValue < desiredValue).ToArray();
            if (!things.Any()) return;

            var item = things.MaxBy(i => i.MarketValue); // MaxBy throws exception when list is empty!!!
            if (item == null) return;

            pawn.inventory.container.TryDrop(item, pawn.Position, ThingPlaceMode.Near, out item);

            var text = "VisitorSatisfied".Translate(pawn.Name.ToStringShort, pawn.Possessive(), pawn.ProSubjCap(), GetItemName(item));
            Messages.Message(text, item, MessageSound.Benefit);
        }

        private static bool WillDrop(Pawn pawn, Thing i)
        {
            return i.def != ThingDefOf.Silver && !i.IsMeal() && !pawn.Bought(i);
        }

        private static string GetItemName(Thing item)
        {
            return Find.ActiveLanguageWorker.PostProcessed(Find.ActiveLanguageWorker.WithIndefiniteArticle(item.Label));
        }

        public override void UpdateAllDuties()
        {
            foreach (Pawn pawn in lord.ownedPawns)
            {
                GuestUtility.AddNeedJoy(pawn);
                GuestUtility.AddNeedComfort(pawn);
                pawn.mindState.duty = new PawnDuty(GuestUtility.relaxDef, FlagLoc, Data.radius);
            }
        }
    }
}
