using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using UnityEngine.Assertions;
using Verse.AI.Group;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Hospitality
{
    internal static class GuestUtility
    {
        public static DutyDef relaxDef = DefDatabase<DutyDef>.GetNamed("Relax");

        private static readonly string labelRecruitSuccess = "LetterLabelMessageRecruitSuccess".Translate(); // from core
        private static readonly string labelRecruitFactionAnger = "LetterLabelRecruitFactionAnger".Translate();
        private static readonly string labelRecruitFactionPlease = "LetterLabelRecruitFactionPlease".Translate();
        private static readonly string labelRecruitFactionChiefAnger = "LetterLabelRecruitFactionChiefAnger".Translate();
        private static readonly string labelRecruitFactionChiefPlease = "LetterLabelRecruitFactionChiefPlease".Translate();
        private static readonly string txtRecruitSuccess = "MessageGuestRecruitSuccess".Translate();
        private static readonly string txtForcedRecruit = "MessageGuestForcedRecruit".Translate();
        private static readonly string txtRecruitFactionAnger = "RecruitFactionAnger".Translate();
        private static readonly string txtRecruitFactionPlease = "RecruitFactionPlease".Translate();
        private static readonly string txtRecruitFactionAngerLeaderless = "RecruitFactionAngerLeaderless".Translate();
        private static readonly string txtRecruitFactionPleaseLeaderless = "RecruitFactionPleaseLeaderless".Translate();
        private static readonly string txtLostGroupFactionAnger = "LostGroupFactionAnger".Translate();
        private static readonly string txtLostGroupFactionAngerLeaderless = "LostGroupFactionAngerLeaderless".Translate();

        private static readonly StatDef statRecruitRelationshipDamage = StatDef.Named("RecruitRelationshipDamage");
        private static readonly StatDef statForcedRecruitRelationshipDamage = StatDef.Named("ForcedRecruitRelationshipDamage");
        private static readonly StatDef statRecruitEffectivity = StatDef.Named("RecruitEffectivity");

        private static readonly SimpleCurve RecruitChanceOpinionCurve = new SimpleCurve
        { new CurvePoint(0f, 5), new CurvePoint(0.5f, 20), new CurvePoint(1f, 30) };

        public static bool IsRelaxing(this Pawn pawn)
        {
            return pawn.mindState.duty != null && pawn.mindState.duty.def == relaxDef;
        }

        public static bool IsTraveling(this Pawn pawn)
        {
            return pawn.mindState.duty != null && pawn.mindState.duty.def == DutyDefOf.TravelOrLeave;
        }

        public static bool MayBuy(this Pawn pawn)
        {
            var guestComp = pawn.GetComp<CompGuest>();
            return guestComp?.ShoppingArea != null;
        }

        public static bool IsGuest(this Pawn pawn)
        {
            try
            {
                if (pawn == null) return false;
                if (pawn.Destroyed) return false;
                if (!pawn.Spawned) return false;
                if (pawn.thingIDNumber == 0) return false; // Yeah, this can happen O.O
                if (pawn.Name == null) return false;
                if (pawn.Dead) return false;
                if (pawn.RaceProps?.Humanlike != true) return false;
                if (pawn.guest == null) return false;
                if (pawn.IsPrisonerOfColony || pawn.Faction == Faction.OfPlayer) return false;
                if (!pawn.IsInVisitState()) return false;
                if (pawn.HostileTo(Faction.OfPlayer)) return false;
                //Log.Message(pawn.NameStringShort+": "+(pawn.mindState.duty!=null?pawn.mindState.duty.def.defName : "null"));
                return true;
            }
            catch(Exception e)
            {
                Log.Warning(pawn.Name.ToStringShort + ": \n" + e.Message);
                //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                //Log.Message("Ticks: "+Find.TickManager.TicksGame);
                return false;
            }
        }

        public static bool IsTrader(this Pawn pawn)
        {
            try
            {
                if (pawn == null) return false;
                if (pawn.Destroyed) return false;
                if (!pawn.Spawned) return false;
                if (pawn.thingIDNumber == 0) return false; // Yeah, this can happen O.O
                if (pawn.Name == null) return false;
                if (pawn.Dead) return false;
                if (pawn.RaceProps?.Humanlike != true) return false;
                if (pawn.guest == null) return false;
                if (pawn.IsPrisonerOfColony || pawn.Faction == Faction.OfPlayer) return false;
                if (pawn.HostileTo(Faction.OfPlayer)) return false;
                if (!pawn.IsInTraderState()) return false;
                return true;
            }
            catch(Exception e)
            {
                Log.Warning(e.Message);
                //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                //Log.Message("Ticks: "+Find.TickManager.TicksGame);
                return false;
            }
        }

        public static int RecruitPenalty(this Pawn guest)
        {
            return Mathf.RoundToInt(guest.GetStatValue(statRecruitRelationshipDamage));
        }

        public static int ForcedRecruitPenalty(this Pawn guest)
        {
            return Mathf.RoundToInt(guest.GetStatValue(statForcedRecruitRelationshipDamage));
        }

        public static int GetFriendsInColony(this Pawn guest)
        {
            float requiredOpinion = GetMinRecruitOpinion(guest);
            return GetPawnsFromBase(guest.MapHeld).Where(p => RelationsUtility.PawnsKnowEachOther(guest, p) && guest.relations.OpinionOf(p) >= requiredOpinion).Sum(pawn => GetRelationValue(pawn, guest));
        }

        private static int GetRelationValue(Pawn pawn, Pawn guest)
        {
            if (guest.relations.DirectRelations.Any(rel => rel.otherPawn == pawn)) return 2;
            return 1;
        }

        private static IEnumerable<Pawn> GetPawnsFromBase(Map mapHeld)
        {
            if (mapHeld == null) yield break;

            foreach (var pawn in mapHeld.mapPawns.FreeColonists) yield return pawn;

            foreach (var pawn in GetNearbyColonists(mapHeld)) yield return pawn;
        }

        private static IEnumerable<Pawn> GetNearbyColonists(Map mapHeld)
        {
            return PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists.Where(p => IsNearby(mapHeld, p));
        }

        private static bool IsNearby(Map mapHeld, Pawn p)
        {
            if (p.Spawned && p.MapHeld.IsPlayerHome) return false;
            var tile = p.GetRootTile();
            if (tile == -1) return false;

            return Find.WorldGrid.ApproxDistanceInTiles(mapHeld.Tile, tile) < 8; // within 3 tiles counts
        }

        public static int GetEnemiesInColony(this Pawn guest)
        {
            return GetPawnsFromBase(guest.MapHeld).Where(p => RelationsUtility.PawnsKnowEachOther(guest, p) && guest.relations.OpinionOf(p) <= MaxOpinionForEnemy).Sum(p => GetRelationValue(p, guest));
        }

        public static int GetMinRecruitOpinion(this Pawn guest)
        {
            var difficulty = guest.RecruitDifficulty(Faction.OfPlayer);

            var adjusted = AdjustDifficulty(difficulty);
            return Mathf.RoundToInt(adjusted);
        }

        private static float AdjustDifficulty(float difficulty)
        {
            return RecruitChanceOpinionCurve.Evaluate(difficulty);
        }

        public static bool ImproveRelationship(this Pawn guest)
        {
            var guestComp = guest.GetComp<CompGuest>();
            return guestComp?.chat == true;
        }

        public static bool TryRecruit(this Pawn guest)
        {
            var guestComp = guest.GetComp<CompGuest>();
            return guestComp?.recruit == true;
        }

        public static bool CanTalkTo(this Pawn talker, Pawn talkee)
        {
            return talker.MapHeld == talkee.MapHeld
                && InteractionUtility.CanInitiateInteraction(talker)
                && InteractionUtility.CanReceiveInteraction(talkee)
                   && (talker.Position - talkee.Position).LengthHorizontalSquared <= 36.0
                   && GenSight.LineOfSight(talker.Position, talkee.Position, talker.MapHeld, true);
        }

        public static bool IsArrived(this Pawn guest)
        {
            var guestComp = guest.GetComp<CompGuest>();
            if (guestComp == null) return false;
            return guestComp.arrived;
        }
        
        public static bool ViableGuestTarget(Pawn guest, bool sleepingIsOk = false)
        {
            return guest.IsGuest() && !guest.Downed && (sleepingIsOk || guest.Awake()) && guest.IsArrived() && !guest.HasDismissiveThought() && !IsInTherapy(guest);
        }

        public static void Arrive(this Pawn pawn)
        {
            try
            {
                pawn.PocketHeadgear();
            }
            catch(Exception e)
            {
                Log.Error($"Failed to pocket headgear:\n{e.Message}");
            }

            // Save trader info
            bool trader = pawn.mindState?.wantsToTradeWithColony == true;
            TraderKindDef traderKindDef = trader ? pawn.trader.traderKind : null;

            pawn.guest?.SetGuestStatus(Faction.OfPlayer);

            // Restore trader info
            if (trader)
            {
                pawn.mindState.wantsToTradeWithColony = trader;
                PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn);
                pawn.trader.traderKind = traderKindDef;
            }

            pawn.GetComp<CompGuest>()?.Arrive();
        }

        public static bool GetVisitScore(this Pawn pawn, out float score)
        {
            var lord = pawn.GetLord();
            if (lord?.CurLordToil is LordToil_VisitPoint lordToil && pawn.Faction != null)
            {
                score = lordToil.GetVisitScore(pawn);
                return true;
            }
            score = 0;
            return false;
        }

        public static void Leave(this Pawn pawn)
        {
            try
            {
                pawn.WearHeadgear();
            }
            catch(Exception e)
            {
                Log.Error($"Failed to wear headgear:\n{e.Message}");
            }

            pawn.needs.AddOrRemoveNeedsAsAppropriate();

            pawn.guest?.SetGuestStatus(null);

            pawn.GetComp<CompGuest>()?.Leave();

            //var reservationManager = pawn.MapHeld.reservationManager;
            //var allReservedThings = reservationManager.AllReservedThings().ToArray();
            //foreach (var t in allReservedThings)
            //{
            //    if (reservationManager.ReservedBy(t, pawn)) reservationManager.Release(t, pawn);
            //}
        }

        private static bool IsInVisitState(this Pawn guest)
        {
            var compGuest = guest?.GetComp<CompGuest>();
            var lord = compGuest?.lord;
            var job = lord?.LordJob;
            return job is LordJob_VisitColony;
        }

        private static bool IsInTraderState(this Pawn guest)
        {
            var compGuest = guest?.GetComp<CompGuest>();
            var lord = compGuest?.lord;
            var job = lord?.LordJob;
            return  job is LordJob_TradeWithColony;
        }

        public static bool HasDismissiveThought(this Pawn guest)
        {
            return guest.needs.mood.thoughts.memories.Memories.Any(t => t.def.defName == "GuestDismissiveAttitude");
        }

        public static Pawn[] GetAllGuests(Map map)
        {
            return map.mapPawns.AllPawnsSpawned.Where(IsGuest).ToArray();
        }

        public static void AddNeedJoy(Pawn pawn)
        {
            if (pawn.needs.joy == null)
            {
                var addNeed = typeof (Pawn_NeedsTracker).GetMethod("AddNeed", BindingFlags.Instance | BindingFlags.NonPublic);
                addNeed.Invoke(pawn.needs, new object[] { DefDatabase<NeedDef>.GetNamed("Joy") });
            }
            pawn.needs.joy.CurLevel = Rand.Range(0, 0.5f);
        }

        public static void AddNeedComfort(Pawn pawn)
        {
            if (pawn.needs.comfort == null)
            {
                var addNeed = typeof (Pawn_NeedsTracker).GetMethod("AddNeed", BindingFlags.Instance | BindingFlags.NonPublic);
                addNeed.Invoke(pawn.needs, new object[] { DefDatabase<NeedDef>.GetNamed("Comfort") });
            }
            pawn.needs.comfort.CurLevel = Rand.Range(0, 0.5f);
        }

        public static Building_GuestBed FindBedFor(this Pawn pawn)
        {
            bool BedValidator(Thing t)
            {
                if (!(t is Building_GuestBed)) return false;
                if (!pawn.CanReserveAndReach(t, PathEndMode.OnCell, Danger.Some)) return false;
                var b = (Building_GuestBed) t;
                if (b.CurOccupant != null) return false;
                if (b.ForPrisoners) return false;
                Find.Maps.ForEach(m => m.reservationManager.ReleaseAllForTarget(b)); // TODO: Put this somewhere smarter
                return (!b.IsForbidden(pawn) && !b.IsBurning());
            }

            var bed = (Building_GuestBed)GenClosest.ClosestThingReachable(pawn.Position, pawn.MapHeld, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.OnCell, TraverseParms.For(pawn), 500f, BedValidator);
            return bed;
        }

        public static void PocketHeadgear(this Pawn pawn)
        {
            if (pawn?.apparel?.WornApparel == null || pawn.inventory?.innerContainer == null) return;

            var headgear = pawn.apparel.WornApparel.Where(CoversHead).ToArray();
            foreach (var apparel in headgear)
            {
                if (pawn.GetInventorySpaceFor(apparel) < 1) continue;

                if (pawn.apparel.TryDrop(apparel, out var droppedApp))
                {
                    bool success = pawn.inventory.innerContainer.TryAddOrTransfer(droppedApp.SplitOff(1));
                    if(!success) pawn.apparel.Wear(droppedApp);
                }
            }
        }

        private static bool CoversHead(this Apparel a)
        {
            return a.def.apparel.bodyPartGroups.Any(
                g =>
                    g == BodyPartGroupDefOf.Eyes || g == BodyPartGroupDefOf.UpperHead
                    || g == BodyPartGroupDefOf.FullHead);
        }

        public static void WearHeadgear(this Pawn pawn)
        {
            if (pawn?.apparel?.WornApparel == null || pawn.inventory?.innerContainer == null) return;

            var container = pawn.inventory.innerContainer;
            var headgear = container.OfType<Apparel>().Where(CoversHead).InRandomOrder().ToArray();
            foreach (var apparel in headgear)
            {
                if (pawn.apparel.CanWearWithoutDroppingAnything(apparel.def))
                {
                    container.Remove(apparel);
                    pawn.apparel.Wear(apparel);
                }
            }
        }

        public static void FixTimetable(this Pawn pawn)
        {
            if (pawn.mindState == null) pawn.mindState = new Pawn_MindState(pawn);
            pawn.timetable = new Pawn_TimetableTracker(pawn) {times = new List<TimeAssignmentDef>(24)};
            for (int i = 0; i < 24; i++)
            {
                var def = TimeAssignmentDefOf.Anything;
                pawn.timetable.times.Add(def);
            }
        }

        public static void FixDrugPolicy(this Pawn pawn)
        {
            //if (pawn.drugs == null) 
            pawn.drugs = new Pawn_DrugPolicyTracker(pawn)
            {
                CurrentPolicy = pawn.GetComp<CompGuest>().GetDrugPolicy(pawn)
            };
        }

        public static void CheckRecruitingSuccessful(this Pawn guest, Pawn recruiter, List<RulePackDef> extraSentencePacks)
        {
            if (!guest.TryRecruit()) return;

            var friends = guest.GetFriendsInColony();
            var friendsRequired = FriendsRequired(guest.MapHeld) + guest.GetEnemiesInColony();
            float friendPercentage = 100f * friends / friendsRequired;

            //Log.Message(String.Format("Recruiting {0}: diff: {1} mood: {2}", guest.NameStringShort,recruitDifficulty, colonyTrust));
            if (friendPercentage > 99)
            {
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDef.Named("RecruitGuest"), KnowledgeAmount.Total);

                Find.LetterStack.ReceiveLetter(labelRecruitSuccess, string.Format(txtRecruitSuccess, guest), LetterDefOf.PositiveEvent, guest, guest.Faction);

                RecruitingSuccess(guest, guest.RecruitPenalty());
            }
            else
            {
                TryPleaseGuest(recruiter, guest, true, extraSentencePacks);
            }
        }

        public static void ForceRecruit(Pawn guest, int recruitPenalty)
        {
            GainThought(guest, ThoughtDef.Named("GuestRecruitmentForced"));
           
            Find.LetterStack.ReceiveLetter(labelRecruitSuccess, string.Format(txtForcedRecruit, guest), LetterDefOf.PositiveEvent, guest, guest.Faction);

            RecruitingSuccess(guest, recruitPenalty);
        }

        private static void RecruitingSuccess(Pawn guest, int recruitPenalty)
        {
            if (guest.Faction != Faction.OfPlayer)
            {
                if (guest.Faction != null)
                {
                    guest.Faction.TryAffectGoodwillWith(Faction.OfPlayer, -recruitPenalty, false, true, null, guest);
                    if (recruitPenalty >= 1)
                    {
                        //Log.Message("txtRecruitFactionAnger");
                        string message;
                        if (guest.Faction.leader != null)
                        {
                            message = string.Format(txtRecruitFactionAnger, guest.Faction.leader.Name, guest.Faction.Name, guest.Name.ToStringShort, GenText.ToStringByStyle(-recruitPenalty, ToStringStyle.Integer, ToStringNumberSense.Offset));
                            Find.LetterStack.ReceiveLetter(labelRecruitFactionChiefAnger, message, LetterDefOf.NegativeEvent, GlobalTargetInfo.Invalid, guest.Faction);
                        }
                        else
                        {
                            message = string.Format(txtRecruitFactionAngerLeaderless, guest.Faction.Name, guest.Name.ToStringShort, GenText.ToStringByStyle(-recruitPenalty, ToStringStyle.Integer, ToStringNumberSense.Offset));
                            Find.LetterStack.ReceiveLetter(labelRecruitFactionAnger, message, LetterDefOf.NegativeEvent, GlobalTargetInfo.Invalid, guest.Faction);
                        }
                    }
                    else if (recruitPenalty <= -1)
                    {
                        //Log.Message("txtRecruitFactionPlease");
                        string message;
                        if (guest.Faction.leader != null)
                        {
                            message = string.Format(txtRecruitFactionPlease, guest.Faction.leader.Name, guest.Faction.Name, guest.Name.ToStringShort, GenText.ToStringByStyle(-recruitPenalty, ToStringStyle.Integer, ToStringNumberSense.Offset));
                            Find.LetterStack.ReceiveLetter(labelRecruitFactionChiefPlease, message, LetterDefOf.PositiveEvent, GlobalTargetInfo.Invalid, guest.Faction);
                        }
                        else
                        {
                            message = string.Format(txtRecruitFactionPleaseLeaderless, guest.Faction.Name, guest.Name.ToStringShort, GenText.ToStringByStyle(-recruitPenalty, ToStringStyle.Integer, ToStringNumberSense.Offset));
                            Find.LetterStack.ReceiveLetter(labelRecruitFactionPlease, message, LetterDefOf.PositiveEvent, GlobalTargetInfo.Invalid, guest.Faction);
                        }
                    }
                }
                guest.Adopt();
            }
            var taleParams = new object[] {guest.MapHeld.mapPawns.FreeColonistsSpawned.RandomElement(), guest};
            TaleRecorder.RecordTale(TaleDef.Named("Recruited"), taleParams);
        }

        public static void Adopt(this Pawn guest)
        {
            // Clear mind
            guest.pather.StopDead();

            // Clear reservations
            Find.Maps.ForEach(m => m.reservationManager.ReleaseAllClaimedBy(guest));

            // Cancel jobs
            if (guest.jobs.jobQueue != null) guest.jobs.jobQueue = new JobQueue();
            guest.jobs.EndCurrentJob(JobCondition.InterruptForced);

            // Reset timetable to default
            guest.timetable = new Pawn_TimetableTracker(guest);

            var lord = guest.GetLord();
            if (lord?.ownedPawns.Count > 1)
            {
                for (int i = guest.inventory.innerContainer.Count - 1; i >= 0; i--)
                {
                    var item = guest.inventory.innerContainer[i];
                    var randomOther = lord.ownedPawns.Where(p => p != guest).RandomElement();
                    guest.inventory.innerContainer.TryTransferToContainer(item, randomOther.inventory.innerContainer);
                }
            }
            guest.inventory.innerContainer.TryDropAll(guest.Position, guest.MapHeld, ThingPlaceMode.Near);


            guest.SetFaction(Faction.OfPlayer);

            guest.mindState.exitMapAfterTick = -99999;
            guest.MapHeld.mapPawns.UpdateRegistryForPawn(guest);

            guest.playerSettings.medCare = MedicalCareCategory.Best;
            guest.playerSettings.AreaRestriction = null;

            guest.caller?.DoCall();
        }

        public static float AdjustPleaseChance(float pleaseChance, Pawn recruiter, Pawn target)
        {
            var opinion = target.relations.OpinionOf(recruiter);
            //Log.Message(String.Format("Opinion of {0} about {1}: {2}", target.NameStringShort,recruiter.NameStringShort, opinion));
            //Log.Message(String.Format("{0} + {1} = {2}", pleaseChance, opinion*0.01f, pleaseChance + opinion*0.01f));
            return pleaseChance * 0.8f + opinion*0.01f;
        }

        public static void GainSocialThought(Pawn initiator, Pawn target, ThoughtDef thoughtDef)
        {
            if (!ThoughtUtility.CanGetThought(target, thoughtDef)) return;

            float impact = initiator.GetStatValue(StatDefOf.SocialImpact);
            var thoughtMemory = (Thought_Memory) ThoughtMaker.MakeThought(thoughtDef);
            thoughtMemory.moodPowerFactor = impact;

            if (thoughtMemory is Thought_MemorySocial thoughtSocialMemory)
            {
                thoughtSocialMemory.opinionOffset *= impact;
            }
            target.needs.mood.thoughts.memories.TryGainMemory(thoughtMemory, initiator);
        }

        public static void GainThought(Pawn target, ThoughtDef thoughtDef)
        {
            if (!ThoughtUtility.CanGetThought(target, thoughtDef)) return;

            var thoughtMemory = (Thought_Memory) ThoughtMaker.MakeThought(thoughtDef);
            target.needs.mood.thoughts.memories.TryGainMemory(thoughtMemory);
        }

        public static bool ShouldRecruit(this Pawn pawn, Pawn guest)
        {
            if (!pawn.IsColonist) return false;
            if (!ViableGuestTarget(guest, true)) return false;
            if (!guest.TryRecruit()) return false;
            if (guest.InMentalState) return false;
            //if (guest.relations.OpinionOf(pawn) >= 100) return false;
            //if (guest.RelativeTrust() < 50) return false;
            if (guest.relations.OpinionOf(pawn) <= -10) return false;
            if (!InteractionUtility.CanInitiateInteraction(pawn)) return false;
            if (!InteractionUtility.CanReceiveInteraction(guest)) return false;
            if (!pawn.HasReserved(guest) && !pawn.CanReserveAndReach(guest, PathEndMode.OnCell, pawn.NormalMaxDanger())) return false;
            if (guest.CurJob?.def.suspendable == false) return false;

            return true;
        }

        public static bool ShouldImproveRelationship(this Pawn pawn, Pawn guest)
        {
            if (!pawn.IsColonist) return false;
            if (!ViableGuestTarget(guest)) return false;
            if (!guest.ImproveRelationship()) return false;
            //if (guest.Faction.ColonyGoodwill >= 100) return false;
            if (guest.relations.OpinionOf(pawn) >= 100) return false;
            if (guest.InMentalState) return false;
            if (!guest.IsInGuestZone(guest)) return false;
            if (!InteractionUtility.CanInitiateInteraction(pawn)) return false;
            if (!InteractionUtility.CanReceiveInteraction(guest)) return false;
            if (!pawn.HasReserved(guest) && !pawn.CanReserveAndReach(guest, PathEndMode.OnCell, pawn.NormalMaxDanger())) return false;
            if (guest.CurJob?.def.suspendable == false) return false;

            return true;
        }

        public static void TryGiveBackpack(this Pawn p)
        {
            var def = DefDatabase<ThingDef>.GetNamed("Apparel_Backpack", false);
            if (def == null) return;

            if (p.inventory.innerContainer.Contains(def)) return;

            ThingDef stuff = GenStuff.RandomStuffFor(def);
            var item = (Apparel)ThingMaker.MakeThing(def, stuff);
            item.stackCount = 1;
            p.apparel.Wear(item, false);
        }

        public static int GetInventorySpaceFor(this Pawn pawn, Thing current)
        {
            // Combat Realism
            var inventory = pawn.GetInventory();
            if (inventory == null) return current.stackCount;

            object[] parameters = {current, 0, false, false};
            var success = (bool)inventory
                .GetType()
                .GetMethod("CanFitInInventory", BindingFlags.Instance | BindingFlags.Public)
                .Invoke(inventory, parameters);
            if (!success) return 0;
            var count = (int) parameters[1];

            return count;
        }

        private static ThingComp GetInventory(this Pawn pawn)
        {
            return pawn.AllComps.FirstOrDefault(c => c.GetType().Name == "CompInventory");
        }

        public static void Break(this Pawn pawn)
        {
            if (!pawn.Spawned || pawn.Dead || pawn.Downed || pawn.InMentalState) return;

            pawn.guest?.SetGuestStatus(null);
            bool canFlee = pawn.Map.reachability.CanReachMapEdge(pawn.PositionHeld, TraverseParms.For(TraverseMode.NoPassClosedDoors));
            
            var mentalState = canFlee ? MentalStateDefOf.PanicFlee : MentalStateDefOf.ManhunterPermanent;

            pawn.mindState.mentalStateHandler.TryStartMentalState(mentalState);
        }

        public static Area GetGuestArea(this Pawn p)
        {
            var compGuest = p.GetComp<CompGuest>();

            return compGuest?.GuestArea;
        }

         public static Area GetShoppingArea(this Pawn p)
        {
            var compGuest = p.GetComp<CompGuest>();

            return compGuest?.ShoppingArea;
        }

        public static bool Bought(this Pawn pawn, Thing thing)
        {
            var comp = pawn.GetComp<CompGuest>();
            if (comp == null) return false;

            //Log.Message(pawn.NameStringShort+": bought "+thing.Label + "? " + (comp.boughtItems.Contains(thing.thingIDNumber) ? "Yes" : "No"));
            return comp.boughtItems.Contains(thing.thingIDNumber);
        }

        public static bool IsInGuestZone(this Pawn p, Thing s)
        {
            var area = p.GetGuestArea();
            if (area == null) return true;
            return area[s.Position];
        }

        public static bool IsInShoppingZone(this Pawn p, Thing s)
        {
            var area = p.GetShoppingArea();
            if (area == null) return false;
            return area[s.Position];
        }

        public static IEnumerable<Building_GuestBed> GetGuestBeds(this Map map, Area area = null)
        {
            if (map == null) return new Building_GuestBed[0];
            if (area == null) return map.listerBuildings.AllBuildingsColonistOfClass<Building_GuestBed>();
            return map.listerBuildings.AllBuildingsColonistOfClass<Building_GuestBed>().Where(b => area[b.Position]);
        }

        public static int FriendsRequired(Map mapHeld)
        {
            var x = GetPawnsFromBase(mapHeld).Count();
            if (x <= 3) return 1;
            // Formula from: https://mycurvefit.com/share/5b359026-5f44-4ac4-88ed-9b364a242f7b
            var a = 0.887f;
            var b = 0.646f;
            var y = a * Mathf.Pow(x, b);
            var required = y;
            return Mathf.RoundToInt(required);
        }

        public static void EndorseColonists(Pawn recruiter, Pawn guest)
        {
            if (guest.relations == null) return;
            if (recruiter.relations == null) return;

            var pawns = guest.MapHeld.mapPawns.FreeColonistsSpawned.Where(c=> c != recruiter && recruiter.relations.OpinionOf(c) > 0).ToArray();
            if (pawns.Length == 0) return;

            if (pawns.TryRandomElement(out var target))
            {
                GainSocialThought(target, guest, ThoughtDef.Named("EndorsedByRecruiter"));

                //Log.Message(recruiter.NameStringShort + " endorsed " + target + " to " + guest.Name);
            }
        }

        public static void TryPleaseGuest(Pawn recruiter, Pawn guest, bool focusOnRecruiting, List<RulePackDef> extraSentencePacks)
        {
            // TODO: pawn.records.Increment(RecordDefOf.GuestsCharmAttempts);
            recruiter.skills.Learn(SkillDefOf.Social, 35f);
            float pleaseChance = recruiter.GetStatValue(StatDefOf.NegotiationAbility);
            pleaseChance = AdjustPleaseChance(pleaseChance, recruiter, guest);
            pleaseChance = Mathf.Clamp01(pleaseChance);

            var failedCharms = guest.GetComp<CompGuest>().failedCharms;

            if (Rand.Value > pleaseChance)
            {
                var isAbrasive = recruiter.story.traits.HasTrait(TraitDefOf.Abrasive);
                int multiplier = isAbrasive ? 2 : 1;
                string multiplierText = multiplier > 1 ? " x" + multiplier : String.Empty;

                if (failedCharms.TryGetValue(recruiter, out var amount))
                {
                    amount++;
                    failedCharms[recruiter] = amount;
                }
                else
                {
                    failedCharms.Add(recruiter, 1);
                }

                if (amount >= 3)
                {
                    Messages.Message(
                        "RecruitAngerMultiple".Translate(recruiter.Name.ToStringShort, guest.Name.ToStringShort, amount),
                        guest, MessageTypeDefOf.NegativeEvent);
                }

                extraSentencePacks.Add(RulePackDef.Named("Sentence_CharmAttemptRejected"));
                for (int i = 0; i < multiplier; i++)
                {
                    GainSocialThought(recruiter, guest, ThoughtDef.Named("GuestOffendedRelationship"));
                }

                MoteMaker.ThrowText((recruiter.DrawPos + guest.DrawPos) / 2f, recruiter.Map, "TextMote_CharmFail".Translate()+multiplierText, 8f);
            }
            else
            {
                failedCharms.Remove(recruiter);

                var statValue = recruiter.GetStatValue(statRecruitEffectivity);
                var floor = Mathf.FloorToInt(statValue);
                int multiplier = floor + (Rand.Value < statValue - floor ? 1 : 0);

                // Multiplier is for what the focus is on
                for (int i = 0; i < multiplier; i++)
                {
                    if(focusOnRecruiting)
                        EndorseColonists(recruiter, guest);
                    else
                        GainSocialThought(recruiter, guest, ThoughtDef.Named("GuestPleasedRelationship"));
                }
                
                // And then one more of the other
                multiplier++; 
                if (focusOnRecruiting)
                    GainSocialThought(recruiter, guest, ThoughtDef.Named("GuestPleasedRelationship"));
                else
                    EndorseColonists(recruiter, guest);

                extraSentencePacks.Add(RulePackDef.Named("Sentence_CharmAttemptAccepted"));

                string multiplierText = multiplier > 1 ? " x" + multiplier : String.Empty;
                MoteMaker.ThrowText((recruiter.DrawPos + guest.DrawPos) / 2f, recruiter.Map, "TextMote_CharmSuccess".Translate() + multiplierText, 8f);
            }
            GainThought(guest, ThoughtDef.Named("GuestDismissiveAttitude"));
        }

        public const int InteractIntervalAbsoluteMin = 360; // changed from 120
        public const int MaxOpinionForEnemy = -20;

        public static void DoAllowedAreaSelectors(Rect rect, Pawn p, Func<Area, string> getLabel)
		{
			if (Find.CurrentMap == null)
			{
				return;
			}
            var areas = GetAreas().ToArray();
            int num = areas.Length + 1;
            float num2 = rect.width / num;
			Text.WordWrap = false;
			Text.Font = GameFont.Tiny;
			Rect rect2 = new Rect(rect.x, rect.y, num2, rect.height);
			DoAreaSelector(rect2, p, null, getLabel);
			int num3 = 1;
			foreach (Area a in areas)
			{
			    float num4 = num3*num2;
			    Rect rect3 = new Rect(rect.x + num4, rect.y, num2, rect.height);
			    DoAreaSelector(rect3, p, a, getLabel);
			    num3++;
			}
            Text.WordWrap = true;
			Text.Font = GameFont.Small;
		}

        public static IEnumerable<Area> GetAreas()
        {
            return Find.CurrentMap.areaManager.AllAreas.Where(a=>a.AssignableAsAllowed());
        }

        // From RimWorld.AreaAllowedGUI, modified
        private static void DoAreaSelector(Rect rect, Pawn p, Area area, Func<Area, string> getLabel)
		{
			rect = rect.ContractedBy(1f);
			GUI.DrawTexture(rect, area == null ? BaseContent.GreyTex : area.ColorTexture);
			Text.Anchor = TextAnchor.MiddleLeft;
			string text = getLabel(area);
			Rect rect2 = rect;
			rect2.xMin += 3f;
			rect2.yMin += 2f;
			Widgets.Label(rect2, text);
			if (p.playerSettings.AreaRestriction == area)
			{
				Widgets.DrawBox(rect, 2);
			}
			if (Mouse.IsOver(rect))
			{
			    area?.MarkForDraw();
			    if (Input.GetMouseButton(0) && p.playerSettings.AreaRestriction != area)
				{
					p.playerSettings.AreaRestriction = area;
					SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera();
				}
			}
			Text.Anchor = TextAnchor.UpperLeft;
			TooltipHandler.TipRegion(rect, text);
		}

        public static readonly JobDef therapyJobDef = DefDatabase<JobDef>.GetNamedSilentFail("ReceiveTherapy");

        // Compatibility fix to Therapy mod
        public static bool IsInTherapy(Pawn p)
        {
            return therapyJobDef != null && p.CurJob != null && p.CurJob.def == therapyJobDef;
        }

        public static bool GuestsShouldStayLonger(Lord lord)
        {
            var map = lord.Map;
            var mentalPawns = map.mapPawns.AllPawnsSpawned.Where(p => !p.Dead && !p.IsPrisoner && !p.Downed && p.MentalState != null && p.InMentalState).ToArray();
            //var temp = faction.def.allowedArrivalTemperatureRange.Includes(map.mapTemperature.OutdoorTemp) && faction.def.allowedArrivalTemperatureRange.Includes(map.mapTemperature.SeasonalTemp);

            return mentalPawns.Length > 0;
        }

        public static void OnLostEntireGroup(Lord lord)
        {
            const int penalty = -10;
            //Log.Message("Lost group");
            if (lord?.faction != null)
            {
                //Log.Message("Had lord and faction");
                lord.faction.TryAffectGoodwillWith(Faction.OfPlayer, penalty, false);
                if (lord.faction.leader == null)
                {
                    var message = string.Format(txtLostGroupFactionAngerLeaderless, lord.faction.Name, GenText.ToStringByStyle(penalty, ToStringStyle.Integer, ToStringNumberSense.Offset));
                    Find.LetterStack.ReceiveLetter(labelRecruitFactionAnger, message, LetterDefOf.NegativeEvent, GlobalTargetInfo.Invalid, lord.faction);
                }
                else
                {
                    var message = string.Format(txtLostGroupFactionAnger, lord.faction.leader.Name, lord.faction.Name, GenText.ToStringByStyle(penalty, ToStringStyle.Integer, ToStringNumberSense.Offset));
                    Find.LetterStack.ReceiveLetter(labelRecruitFactionChiefAnger, message, LetterDefOf.NegativeEvent, GlobalTargetInfo.Invalid, lord.faction);
                }
            }
        }

        public static void RefuseGuestsUntilWeHaveBeds(Map map)
        {
            if (map == null) return;

            var mapComp = Hospitality_MapComponent.Instance(map);
            mapComp.refuseGuestsUntilWeHaveBeds = true;
            LessonAutoActivator.TeachOpportunity(ConceptDef.Named("GuestBeds"), null, OpportunityType.Important);
        }

        public static bool BedCheck(Map map)
        {
            if (map == null) return false;
            var mapComp = Hospitality_MapComponent.Instance(map);

            if (!mapComp.refuseGuestsUntilWeHaveBeds) return true;
            if (!map.listerBuildings.AllBuildingsColonistOfClass<Building_GuestBed>().Any()) return false;

            // We have beds now!
            mapComp.refuseGuestsUntilWeHaveBeds = false;
            return true;
        }
    }
}
