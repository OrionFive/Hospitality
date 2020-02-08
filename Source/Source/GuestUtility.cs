using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse.AI.Group;
using Verse;
using Verse.AI;
using Verse.Sound;
using static System.String;

namespace Hospitality
{
    internal static class GuestUtility
    {
        public static DutyDef relaxDef = DefDatabase<DutyDef>.GetNamed("Relax");
        private static readonly TraderKindDef traderKindDef = DefDatabase<TraderKindDef>.GetNamed("Guest");

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
                if (!IsValidPawn(pawn)) return false;
                if (!pawn.IsInVisitState()) return false;
                return true;
            }
            catch(Exception e)
            {
                Log.Warning(pawn.Name.ToStringShort + ": \n" + e.Message);
                return false;
            }
        }

        public static bool IsTrader(this Pawn pawn)
        {
            try
            {
                if (!IsValidPawn(pawn)) return false;
                if (!pawn.IsInTraderState()) return false;
                return true;
            }
            catch(Exception e)
            {
                Log.Warning(e.Message);
                return false;
            }
        }

        public static bool IsValidPawn(this Pawn pawn)
        {
            if (pawn == null) return false;
            if (pawn.Destroyed) return false;
            if (!pawn.Spawned) return false;
            if (pawn.thingIDNumber == 0) return false;
            if (pawn.Name == null) return false;
            if (pawn.Dead) return false;
            if (pawn.RaceProps?.Humanlike != true) return false;
            if (pawn.guest == null) return false;
            if (pawn.guest.HostFaction != Faction.OfPlayer && pawn.Map.ParentFaction != Faction.OfPlayer) return false;
            if (pawn.Faction == null) return false;
            if (pawn.IsPrisonerOfColony || pawn.Faction == Faction.OfPlayer) return false;
            if (pawn.HostileTo(Faction.OfPlayer)) return false;
            return true;
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
            if (guest.relations.RelatedPawns.Any(rel => rel == pawn)) return 2;
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
            return guestComp?.entertain == true;
        }

        public static bool MakeFriends(this Pawn guest)
        {
            var guestComp = guest.GetComp<CompGuest>();
            return guestComp?.makeFriends == true;
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
            return guest.IsGuest() && !guest.Downed && (sleepingIsOk || guest.Awake()) && guest.IsArrived() && !guest.HasDismissiveThought() && !IsInTherapy(guest) && !IsTired(guest) && !IsEating(guest);
        }

        private static bool IsEating(Pawn guest)
        {
            return guest.CurJobDef == JobDefOf.Ingest;
        }

        public static bool IsTired(this Pawn pawn)
        {
            return pawn?.needs?.rest?.CurCategory >= RestCategory.VeryTired;
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

        private static bool IsInVisitState(this Pawn pawn)
        {
            var compGuest = pawn?.GetComp<CompGuest>();
            var lord = compGuest?.lord;
            if (lord == null) return true;
            //if (!pawn.Map.lordManager.lords.Contains(lord)) return false; // invalid lord
            var job = lord?.LordJob;
            return job is LordJob_VisitColony;
        }

        private static bool IsInTraderState(this Pawn pawn)
        {
            var compGuest = pawn?.GetComp<CompGuest>();
            var lord = compGuest?.lord;
            //if (!pawn.Map.lordManager.lords.Contains(lord)) return false; // invalid lord
            var job = lord?.LordJob;
            return job is LordJob_TradeWithColony;
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

        public static void TryImproveFriendship(this Pawn guest, Pawn recruiter, List<RulePackDef> extraSentencePacks)
        {
            if (!guest.MakeFriends()) return;

            var friendPercentage = GetFriendPercentage(guest);

            //Log.Message(String.Format("Recruiting {0}: diff: {1} mood: {2}", guest.NameStringShort,recruitDifficulty, colonyTrust));
            TryPleaseGuest(recruiter, guest, friendPercentage < 1, extraSentencePacks);

            // Notify player if the guest can be recruited now
            if (friendPercentage < 1 && Settings.enableRecruitNotification)
            {
                var newFriendPercentage = GetFriendPercentage(guest);
                if (newFriendPercentage >= 1)
                {
                    Messages.Message("GuestCanBeRecruitedNow".Translate(new NamedArgument {arg = guest, label = "PAWN"}).AdjustedFor(guest), guest, MessageTypeDefOf.NeutralEvent);
                }
            }
        }

        private static float GetFriendPercentage(Pawn guest)
        {
            var friends = guest.GetFriendsInColony();
            var friendsRequired = FriendsRequired(guest.MapHeld) + guest.GetEnemiesInColony();
            return 1f * friends / friendsRequired;
        }

        public static void Recruit(Pawn guest, int recruitPenalty, bool forced)
        {
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDef.Named("RecruitGuest"), KnowledgeAmount.Total);

            if(forced)
                GainThought(guest, ThoughtDef.Named("GuestRecruitmentForced"));

            Find.LetterStack.ReceiveLetter(labelRecruitSuccess, Format(forced ? txtForcedRecruit : txtRecruitSuccess, guest), LetterDefOf.PositiveEvent, guest, guest.Faction);

            AngerFactionMembers(guest);
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
                            message = Format(txtRecruitFactionAnger, guest.Faction.leader.Name, guest.Faction.Name, guest.Name.ToStringShort, GenText.ToStringByStyle(-recruitPenalty, ToStringStyle.Integer, ToStringNumberSense.Offset));
                            Find.LetterStack.ReceiveLetter(labelRecruitFactionChiefAnger, message, LetterDefOf.NegativeEvent, GlobalTargetInfo.Invalid, guest.Faction);
                        }
                        else
                        {
                            message = Format(txtRecruitFactionAngerLeaderless, guest.Faction.Name, guest.Name.ToStringShort, GenText.ToStringByStyle(-recruitPenalty, ToStringStyle.Integer, ToStringNumberSense.Offset));
                            Find.LetterStack.ReceiveLetter(labelRecruitFactionAnger, message, LetterDefOf.NegativeEvent, GlobalTargetInfo.Invalid, guest.Faction);
                        }
                    }
                    else if (recruitPenalty <= -1)
                    {
                        //Log.Message("txtRecruitFactionPlease");
                        string message;
                        if (guest.Faction.leader != null)
                        {
                            message = Format(txtRecruitFactionPlease, guest.Faction.leader.Name, guest.Faction.Name, guest.Name.ToStringShort, GenText.ToStringByStyle(-recruitPenalty, ToStringStyle.Integer, ToStringNumberSense.Offset));
                            Find.LetterStack.ReceiveLetter(labelRecruitFactionChiefPlease, message, LetterDefOf.PositiveEvent, GlobalTargetInfo.Invalid, guest.Faction);
                        }
                        else
                        {
                            message = Format(txtRecruitFactionPleaseLeaderless, guest.Faction.Name, guest.Name.ToStringShort, GenText.ToStringByStyle(-recruitPenalty, ToStringStyle.Integer, ToStringNumberSense.Offset));
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

            guest.ownership.UnclaimBed();
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

        private static void GainSocialThought(Pawn initiator, Pawn target, ThoughtDef thoughtDef)
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

        public static void UpsetAboutFee(this Pawn pawn, int cost)
        {
            var thoughtDef = ThoughtDef.Named("GuestPaidFee");
            var amount = cost / 10;
            for (int i = 0; i < amount; i++)
            {
                var thoughtMemory = (Thought_Memory) ThoughtMaker.MakeThought(thoughtDef);
                pawn?.needs?.mood?.thoughts?.memories?.TryGainMemory(thoughtMemory); // *cough* Extra defensive
            }
        }

        private static void GainThought(this Pawn target, ThoughtDef thoughtDef)
        {
            if (!ThoughtUtility.CanGetThought(target, thoughtDef)) return;

            var thoughtMemory = (Thought_Memory) ThoughtMaker.MakeThought(thoughtDef);
            target?.needs?.mood?.thoughts?.memories?.TryGainMemory(thoughtMemory); // *cough* Extra defensive
        }

        public static bool ShouldMakeFriends(this Pawn pawn, Pawn guest)
        {
            if (!pawn.IsColonist) return false;
            if (!ViableGuestTarget(guest, true)) return false;
            if (!guest.MakeFriends()) return false;
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
                string multiplierText = multiplier > 1 ? " x" + multiplier : Empty;

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

                string multiplierText = multiplier > 1 ? " x" + multiplier : Empty;
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
            var incapableToLeave = lord.ownedPawns.Where(p => !p.Dead && !p.IsPrisoner && (p.Downed || p.MentalState != null && p.InMentalState));
            return incapableToLeave.Any();
        }

        public static void OnLostEntireGroup(Lord lord)
        {
            // Check if we should get upset
            if (lord?.LordJob is LordJob_VisitColony job && !job.getUpsetWhenLost) return;

            const int penalty = -10;
            //Log.Message("Lost group");
            if (lord?.faction != null)
            {
                //Log.Message("Had lord and faction");
                lord.faction.TryAffectGoodwillWith(Faction.OfPlayer, penalty, false);
                if (lord.faction.leader == null)
                {
                    var message = Format(txtLostGroupFactionAngerLeaderless, lord.faction.Name, GenText.ToStringByStyle(penalty, ToStringStyle.Integer, ToStringNumberSense.Offset));
                    Find.LetterStack.ReceiveLetter(labelRecruitFactionAnger, message, LetterDefOf.NegativeEvent, GlobalTargetInfo.Invalid, lord.faction);
                }
                else
                {
                    var message = Format(txtLostGroupFactionAnger, lord.faction.leader.Name, lord.faction.Name, GenText.ToStringByStyle(penalty, ToStringStyle.Integer, ToStringNumberSense.Offset));
                    Find.LetterStack.ReceiveLetter(labelRecruitFactionChiefAnger, message, LetterDefOf.NegativeEvent, GlobalTargetInfo.Invalid, lord.faction);
                }
            }
        }

        private static void AngerFactionMembers(Pawn guest)
        {
            if (guest.Faction == null || guest.Faction == Faction.OfPlayer) return;

            var map = guest.MapHeld;
            var allies = map.mapPawns.PawnsInFaction(guest.Faction).ToArray();
            foreach (var ally in allies)
            {
                if (ally != guest && !ally.Dead && ally.Spawned)
                {
                    GainThought(ally, ThoughtDef.Named("GuestAngered"));
                    GainThought(ally, ThoughtDef.Named("GuestDismissiveAttitude"));
                }
            }
        }

        public static void CheckForRogueGuests(Map map)
        {
            var pawns = map.mapPawns.AllPawnsSpawned.Where(p => p.GetPosture() == PawnPosture.Standing && !HealthAIUtility.ShouldSeekMedicalRest(p) && !p.health.hediffSet.HasNaturallyHealingInjury()).Where(GuestHasNoLord).ToArray();

            foreach (var pawn in pawns)
            {
                if (pawn == null) continue; // I don't think this ever happens...
                if (pawn.mindState.duty?.def == DutyDefOf.ExitMapBestAndDefendSelf) continue;

                var lords = map.lordManager.lords.Where(lord => lord.CurLordToil is LordToil_VisitPoint && lord.faction == pawn.Faction).ToArray();
                if (lords.Any())
                {
                    JoinLord(lords.RandomElement(), pawn);
                }
                else CreateLordForPawn(pawn);

                pawn.jobs.StopAll();
                pawn.pather.StopDead();
            }
        }

        private static void CreateLordForPawn([NotNull] Pawn pawn)
        {
            Log.Message($"Creating a temporary lord for {pawn.Label} of faction {(pawn.Faction != null ? pawn.Faction.Name : "null")}.");
            Find.LetterStack.ReceiveLetter("LetterLabelDownedPawnBecameGuest".Translate(new NamedArgument {arg = pawn, label = "PAWN"}), "DownedPawnBecameGuest".Translate(new NamedArgument {arg = pawn, label = "PAWN"}), LetterDefOf.NeutralEvent, pawn, pawn.Faction);
            var duration = (int)(Rand.Range(0.5f, 1f) * GenDate.TicksPerDay);
            IncidentWorker_VisitorGroup.CreateLord(pawn.Faction, pawn.Position, new List<Pawn> {pawn}, pawn.Map, false, false, duration);
        }

        private static bool GuestHasNoLord(Pawn pawn)
        {
            if (!IsValidPawn(pawn)) return false;
            if (IsInTraderState(pawn)) return false;

            var comp = pawn.GetComp<CompGuest>();
            if (comp == null) return false;

            var mapLord = pawn.GetLord(); // Expensive :(
            if (mapLord != null && mapLord.Map != pawn.Map) Log.Warning($"{pawn.Name.ToStringFull}'s lord is on a different map!");
            return mapLord == null;
        }

        private static void JoinLord(Lord lord, Pawn pawn)
        {
            if (lord.ownedPawns.Contains(pawn))
            {
                pawn.GetComp<CompGuest>().lord = lord;
                return;
            }
            if (!(lord.CurLordToil is LordToil_VisitPoint lordToil)) return;

            Log.Message($"{pawn.LabelShort}: Joined lord of faction {lord.faction?.Name}.");
            Find.LetterStack.ReceiveLetter("LetterLabelDownedPawnJoinedGroup".Translate(new NamedArgument {arg = pawn, label = "PAWN"}), "DownedPawnJoinedGroup".Translate(new NamedArgument {arg = pawn, label = "PAWN"}), LetterDefOf.NeutralEvent, pawn, pawn.Faction);
            lordToil.JoinLate(pawn);
        }

        public static void ConvertToTrader(this Pawn pawn, bool actAsIfSpawned)
        {
            pawn.mindState.wantsToTradeWithColony = true;
            PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn, actAsIfSpawned);
            pawn.trader.traderKind = traderKindDef;
        }
    }
}
