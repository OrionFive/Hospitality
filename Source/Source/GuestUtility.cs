using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse.AI.Group;
using Verse;
using Verse.AI;

namespace Hospitality
{
    internal static class GuestUtility
    {
        public static DutyDef relaxDef = DefDatabase<DutyDef>.GetNamed("Relax");
        public static DutyDef travelDef = DefDatabase<DutyDef>.GetNamed("Travel");

        private static readonly string labelRecruitSuccess = "LetterLabelMessageRecruitSuccess".Translate(); // from core
        private static readonly string labelRecruitFactionAnger = "LetterLabelRecruitFactionAnger".Translate();
        private static readonly string labelRecruitFactionPlease = "LetterLabelRecruitFactionPlease".Translate();
        private static readonly string labelRecruitFactionChiefAnger = "LetterLabelRecruitFactionChiefAnger".Translate();
        private static readonly string labelRecruitFactionChiefPlease = "LetterLabelRecruitFactionChiefPlease".Translate();
        private static readonly string txtRecruitSuccess = "MessageGuestRecruitSuccess".Translate();
        private static readonly string txtRecruitFactionAnger = "RecruitFactionAnger".Translate();
        private static readonly string txtRecruitFactionPlease = "RecruitFactionPlease".Translate();
        private static readonly string txtRecruitFactionAngerLeaderless = "RecruitFactionAngerLeaderless".Translate();
        private static readonly string txtRecruitFactionPleaseLeaderless = "RecruitFactionPleaseLeaderless".Translate();

        private static readonly StatDef statRecruitRelationshipDamage = StatDef.Named("RecruitRelationshipDamage");

        public static bool IsRelaxing(this Pawn pawn)
        {
            return pawn.mindState.duty != null && pawn.mindState.duty.def == relaxDef;
        }

        public static bool IsTraveling(this Pawn pawn)
        {
            return pawn.mindState.duty != null && pawn.mindState.duty.def == travelDef;
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
                if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike) return false;
                if (pawn.guest == null) return false;
                if (pawn.IsPrisonerOfColony || pawn.Faction == Faction.OfPlayer) return false;
                if (pawn.HostileTo(Faction.OfPlayer)) return false;
                if (!pawn.IsInVisitState()) return false;
                //Log.Message(pawn.NameStringShort+": "+(pawn.mindState.duty!=null?pawn.mindState.duty.def.defName : "null"));
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

        public static float RecruitPenalty(this Pawn guest)
        {
            return guest.GetStatValue(statRecruitRelationshipDamage);
        }

        public static float RelativeTrust(this Pawn guest)
        {
            var difficulty = guest.RecruitDifficulty(Faction.OfPlayer, true);
            var trust = guest.CalculateColonyTrust() / difficulty;
            return trust;
        }

        public static bool ImproveRelationship(this Pawn guest)
        {
            var guestComp = guest.GetComp<CompGuest>();
            if (guestComp == null) return false;
            return guestComp.chat;
        }

        public static bool TryRecruit(this Pawn guest)
        {
            var guestComp = guest.GetComp<CompGuest>();
            if (guestComp == null) return false;
            return guestComp.recruit;
        }

        public static bool CanTalkTo(this Pawn talker, Pawn talkee)
        {
            return InteractionUtility.CanInitiateInteraction(talker)
                && InteractionUtility.CanReceiveInteraction(talkee)
                   && (talker.Position - talkee.Position).LengthHorizontalSquared <= 36.0
                   && GenSight.LineOfSight(talker.Position, talkee.Position, true);
        }
        
        public static bool ViableGuestTarget(Pawn guest, bool sleepingIsOk = false)
        {
            return !(!guest.IsGuest() || guest.Downed || (!sleepingIsOk && !guest.Awake()) || !Find.AreaHome[guest.Position] || guest.HasDismissiveThought());
        }

        private static bool IsInVisitState(this Pawn guest)
        {
            var lord = guest.GetLord();
            if (lord == null) return false;

            var job = lord.LordJob;
            return  job is LordJob_VisitColony;
        }

        public static bool HasDismissiveThought(this Pawn guest)
        {
            return guest.needs.mood.thoughts.Thoughts.Any(t => t.def.defName == "GuestDismissiveAttitude");
        }

        public static Pawn[] GetAllGuests()
        {
            return Find.MapPawns.AllPawnsSpawned.Where(IsGuest).ToArray();
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
            Predicate<Thing> bedValidator = delegate(Thing t)
                                            {
                                                if (!pawn.CanReserveAndReach(t, PathEndMode.OnCell, Danger.Some)) return false;
                                                var b = (Building_GuestBed) t;
                                                if (b.CurOccupant != null) return false;
                                                if (b.ForPrisoners) return false;
                                                Find.Reservations.ReleaseAllForTarget(b);
                                                return (!b.IsForbidden(pawn) && !b.IsBurning());
                                            };
            var thingDef = ThingDef.Named("GuestBed");
            var bed = (Building_GuestBed) GenClosest.ClosestThingReachable(pawn.GetLord().CurLordToil.FlagLoc, ThingRequest.ForDef(thingDef), PathEndMode.OnCell, TraverseParms.For(pawn), 500f, bedValidator);
            if (bed != null) return bed;
            return null;
        }

        public static void PocketHeadgear(this Pawn pawn)
        {
            var headgear = pawn.apparel.WornApparel.Where(CoversHead).ToArray();
            foreach (var apparel in headgear)
            {
                if (pawn.GetInventorySpaceFor(apparel) < 1) continue;
                
                Apparel droppedApp;
                if (pawn.apparel.TryDrop(apparel, out droppedApp))
                {
                    bool success = pawn.inventory.container.TryAdd(droppedApp);
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
            var headgear = pawn.inventory.container.OfType<Apparel>().Where(CoversHead).InRandomOrder().ToArray();
            foreach (var apparel in headgear)
            {
                if (pawn.apparel.CanWearWithoutDroppingAnything(apparel.def))
                {
                    pawn.apparel.Wear(apparel);
                    pawn.inventory.container.Remove(apparel);
                }
            }
        }

        public static void FixTimetable(this Pawn pawn)
        {
            if (pawn.mindState == null) pawn.mindState = new Pawn_MindState(pawn);
            pawn.timetable=new Pawn_TimetableTracker {times = new List<TimeAssignmentDef>(24)};
            for (int i = 0; i < 24; i++)
            {
                var def = TimeAssignmentDefOf.Anything;
                pawn.timetable.times.Add(def);
            }
        }

        public static void FixDrugPolicy(this Pawn pawn)
        {
            //if (pawn.drugs != null) pawn.drugs = null;
            //return;

            if (pawn.drugs == null) pawn.drugs = new Pawn_DrugPolicyTracker(pawn);
            if(pawn.drugs.CurrentPolicy == null) pawn.drugs.CurrentPolicy = new DrugPolicy();
            pawn.drugs.CurrentPolicy.InitializeIfNeeded();
        }

        public static bool CheckRecruitingSuccessful(this Pawn guest, Pawn recruiter)
        {
            if (!guest.TryRecruit()) return false;

            var trust = guest.RelativeTrust();
            //Log.Message(String.Format("Recruiting {0}: diff: {1} mood: {2}", guest.NameStringShort,recruitDifficulty, colonyTrust));
            var chance = Rand.Gaussian(trust, 50);
            if (chance >= 100)
            {
                RecruitingSuccess(guest);
                return true;
            }
            else
            {
                GainSocialThought(recruiter, guest, ThoughtDef.Named("GuestDismissiveAttitude"));
                return false;
            }
        }

        public static float CalculateColonyTrust(this Pawn guest)
        {
            const int requiredAvgOpinion = 1;
            var sum = Find.MapPawns.FreeColonists.Where(p => RelationsUtility.PawnsKnowEachOther(guest, p)).Sum(p => guest.relations.OpinionOf(p)/requiredAvgOpinion);
            //var average = sum/Find.MapPawns.FreeColonistsCount;

            float score = Mathf.Abs(sum) * sum * 1f / Find.MapPawns.FreeColonistsCount;
            //Log.Message(string.Format("{0}: sum = {1:F}, avg = {2:F}, score = {3:F}", guest.NameStringShort, sum, average, score));

            return score;
        }

        private static void RecruitingSuccess(Pawn guest)
        {
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDef.Named("RecruitGuest"), KnowledgeAmount.Total);

            Find.LetterStack.ReceiveLetter(labelRecruitSuccess, String.Format(txtRecruitSuccess, guest), LetterType.Good, guest);
            //if (guest.JailerFaction != null)

            if (guest.Faction != Faction.OfPlayer)
            {
                if (guest.Faction != null)
                {
                    guest.Faction.AffectGoodwillWith(Faction.OfPlayer, -guest.RecruitPenalty());
                    if (guest.RecruitPenalty() >= 1)
                    {
                        //Log.Message("txtRecruitFactionAnger");
                        string message;
                        if (guest.Faction.leader != null)
                        {
                            message = String.Format(txtRecruitFactionAnger, guest.Faction.leader.Name, guest.Faction.Name, guest.NameStringShort, (-guest.RecruitPenalty()).ToStringByStyle(ToStringStyle.Integer, ToStringNumberSense.Offset));
                            Find.LetterStack.ReceiveLetter(labelRecruitFactionChiefAnger, message, LetterType.BadNonUrgent);
                        }
                        else
                        {
                            message = String.Format(txtRecruitFactionAngerLeaderless, guest.Faction.Name, guest.NameStringShort, (-guest.RecruitPenalty()).ToStringByStyle(ToStringStyle.Integer, ToStringNumberSense.Offset));
                            Find.LetterStack.ReceiveLetter(labelRecruitFactionAnger, message, LetterType.BadNonUrgent);
                        }
                    }
                    else if (guest.RecruitPenalty() <= -1)
                    {
                        //Log.Message("txtRecruitFactionPlease");
                        string message;
                        if (guest.Faction.leader != null)
                        {
                            message = String.Format(txtRecruitFactionPlease, guest.Faction.leader.Name, guest.Faction.Name, guest.NameStringShort, (-guest.RecruitPenalty()).ToStringByStyle(ToStringStyle.Integer, ToStringNumberSense.Offset));
                            Find.LetterStack.ReceiveLetter(labelRecruitFactionChiefPlease, message, LetterType.Good);
                        }
                        else
                        {
                            message = String.Format(txtRecruitFactionPleaseLeaderless, guest.Faction.Name, guest.NameStringShort, (-guest.RecruitPenalty()).ToStringByStyle(ToStringStyle.Integer, ToStringNumberSense.Offset));
                            Find.LetterStack.ReceiveLetter(labelRecruitFactionPlease, message, LetterType.Good);
                        }
                    }
                }
                guest.Adopt(Faction.OfPlayer);
            }
            var taleParams = new object[] {Find.MapPawns.FreeColonistsSpawned.RandomElement(), guest};
            TaleRecorder.RecordTale(TaleDef.Named("Recruited"), taleParams);
        }

        public static void Adopt(this Pawn guest, Faction newFaction)
        {
            // Clear mind
            guest.pather.StopDead();
            if (guest.jobQueue != null) guest.jobQueue.Clear();
            guest.jobs.EndCurrentJob(JobCondition.InterruptForced);

            guest.inventory.container.TryDropAll(guest.Position, ThingPlaceMode.Near);

            // Clear reservations
            Find.Reservations.ReleaseAllClaimedBy(guest);

            guest.SetFaction(newFaction);

            guest.mindState.exitMapAfterTick = -99999;
            Find.MapPawns.UpdateRegistryForPawn(guest);

            guest.playerSettings.medCare = MedicalCareCategory.Best;

            if (guest.caller != null) guest.caller.DoCall();
        }

        public static float AdjustPleaseChance(float pleaseChance, Pawn recruiter, Pawn target)
        {
            var opinion = target.relations.OpinionOf(recruiter);
            //Log.Message(String.Format("Opinion of {0} about {1}: {2}", target.NameStringShort,recruiter.NameStringShort, opinion));
            //Log.Message(String.Format("{0} + {1} = {2}", pleaseChance, opinion*0.01f, pleaseChance + opinion*0.01f));
            return pleaseChance + opinion*0.01f;
        }

        public static void GainSocialThought(Pawn initiator, Pawn target, ThoughtDef thoughtDef)
        {
            float impact = initiator.GetStatValue(StatDefOf.SocialImpact);
            Thought_Memory thoughtMemory = (Thought_Memory) ThoughtMaker.MakeThought(thoughtDef);
            thoughtMemory.moodPowerFactor = impact;
            
            var thoughtSocialMemory = thoughtMemory as Thought_MemorySocial;
            if (thoughtSocialMemory != null)
            {
                thoughtSocialMemory.SetOtherPawn(initiator);
                thoughtSocialMemory.opinionOffset *= impact;
            }
            target.needs.mood.thoughts.memories.TryGainMemoryThought(thoughtMemory);
        }

        public static bool ShouldRecruit(this Pawn pawn, Pawn guest)
        {
            if (!ViableGuestTarget(guest, true)) return false;
            if (!guest.TryRecruit()) return false;
            if (guest.InMentalState) return false;
            //if (guest.relations.OpinionOf(pawn) >= 100) return false;
            if (guest.RelativeTrust() < 50) return false;
            if (guest.relations.OpinionOf(pawn) <= -10) return false;
            if (guest.interactions.InteractedTooRecentlyToInteract()) return false;
            if (pawn.interactions.InteractedTooRecentlyToInteract()) return false;
            if (!guest.Awake()) return false;
            if (!pawn.CanReserveAndReach(guest, PathEndMode.OnCell, pawn.NormalMaxDanger())) return false;

            return true;
        }

        public static bool ShouldImproveRelationship(this Pawn pawn, Pawn guest)
        {
            if (!ViableGuestTarget(guest)) return false;
            if (!guest.ImproveRelationship()) return false;
            //if (guest.Faction.ColonyGoodwill >= 100) return false;
            if (guest.relations.OpinionOf(pawn) >= 100) return false;
            if (guest.InMentalState) return false;
            if (guest.interactions.InteractedTooRecentlyToInteract()) return false;
            if (pawn.interactions.InteractedTooRecentlyToInteract()) return false;
            if (!pawn.CanReserveAndReach(guest, PathEndMode.OnCell, pawn.NormalMaxDanger())) return false;

            return true;
        }

        public static void TryGiveBackpack(this Pawn p)
        {
            var def = DefDatabase<ThingDef>.GetNamed("Apparel_Backpack", false);
            if (def == null) return;

            if (p.inventory.container.Contains(def)) return;

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

            var mentalState = MentalStateDefOf.Berserk;
            switch (Rand.Range(0, 3))
            {
                case 0:
                    mentalState = MentalStateDefOf.Berserk;
                    break;
                case 1:
                    mentalState = MentalStateDefOf.Manhunter;
                    break;
                case 2:
                    mentalState = MentalStateDefOf.PanicFlee;
                    break;
            }
            pawn.mindState.mentalStateHandler.TryStartMentalState(mentalState);
        }

        public static void ShowRescuedPawnDialog(Pawn pawn)
        {
            string textAsk = "RescuedInitial".Translate(pawn.story.adulthood.title.ToLower(), GenText.ToCommaList(pawn.story.traits.allTraits.Select(t=>t.Label)));
            textAsk = textAsk.AdjustedFor(pawn);
            PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref textAsk, pawn);
            DiaNode nodeAsk = new DiaNode(textAsk);
            var textAccept = "RescuedInitial_Accept".Translate();
            textAccept = textAccept.AdjustedFor(pawn);

            DiaOption optionAccept = new DiaOption(textAccept);
            optionAccept.action = () => {
                pawn.Adopt(Faction.OfPlayer);
                Find.CameraDriver.JumpTo(pawn.Position);
                Find.LetterStack.ReceiveLetter(labelRecruitSuccess, string.Format(txtRecruitSuccess, pawn),
                    LetterType.Good, pawn);
            };
            optionAccept.resolveTree = true;
            nodeAsk.options.Add(optionAccept);

            var textReject = "RescuedInitial_Reject".Translate();
            textReject = textReject.AdjustedFor(pawn);

            DiaOption optionReject = new DiaOption(textReject);
            optionReject.action = null;
            optionReject.resolveTree = true;

            nodeAsk.options.Add(optionReject);
            Find.WindowStack.Add(new Dialog_NodeTree(nodeAsk, true));
        }

        public static void BreakupRelations(Pawn pawn)
        {
            var relations = pawn.relations.DirectRelations.Where(r => !r.otherPawn.Dead && r.otherPawn.Faction != null && r.otherPawn.Faction.IsPlayer && LovePartnerRelationUtility.LovePartnerRelationExists(pawn, r.otherPawn)).ToArray();
            var breakup = new InteractionWorker_Breakup();
            foreach (var relation in relations)
            {
                breakup.Interacted(relation.otherPawn, pawn, null);
            }
            Faction hostileFaction;
            if (
                Find.FactionManager.AllFactions.Where(f => f.def.humanlikeFaction && f.HostileTo(Faction.OfPlayer))
                    .TryRandomElement(out hostileFaction))
            {
                pawn.SetFaction(hostileFaction);
            }
        }

        public static Room GetGuestRoom(this Pawn p)
        {
            return p.GetLord().CurLordToil.FlagLoc.GetRoom();
        }

        public static bool Bought(this Pawn pawn, Thing thing)
        {
            var comp = pawn.GetComp<CompGuest>();
            if (comp == null) return false;

            //Log.Message(pawn.NameStringShort+": bought "+thing.Label + "? " + (comp.boughtItems.Contains(thing.thingIDNumber) ? "Yes" : "No"));
            return comp.boughtItems.Contains(thing.thingIDNumber);
        }
    }
}