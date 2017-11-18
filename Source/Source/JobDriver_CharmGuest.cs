using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public class JobDriver_CharmGuest : JobDriver_GuestBase
    {
        private static readonly string txtRecruitAngerOther = "RecruitAngerOther".Translate();

        private static readonly StatDef statPleaseGuestChance = StatDef.Named("PleaseGuestChance");

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //this.FailOn(FailCondition);
            var gotoGuest = GotoGuest(pawn, Talkee); // Jump target
            yield return gotoGuest;
            yield return Toils_Interpersonal.WaitToBeAbleToInteract(pawn);
            yield return Toils_Reserve.ReserveQueue(TargetIndex.A);

            yield return Interact(Talkee, InteractionDefOf.RecruitAttempt, GuestUtility.InteractIntervalAbsoluteMin).JumpIf(() => IsBusy(pawn) || IsBusy(Talkee), gotoGuest);
            yield return TryRecruitGuest(pawn, Talkee);
            
            yield return RiskAnger(pawn, Talkee);
            yield return Toils_Interpersonal.SetLastInteractTime(TargetIndex.A);
        }

        public Toil TryRecruitGuest(Pawn recruiter, Pawn guest)
        {
            var toil = new Toil
            {
                initAction = () => {
                    if (!recruiter.ShouldRecruit(guest)) return;
                    if (!recruiter.CanTalkTo(guest)) return;
                    InteractionDef intDef = DefDatabase<InteractionDef>.GetNamed("CharmGuestAttempt");
                    recruiter.interactions.TryInteractWith(guest, intDef);
                    //guest.CheckRecruitingSuccessful(recruiter);
                },
                socialMode = RandomSocialMode.Off,
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = 350
            };
            toil.AddFailCondition(FailCondition);
            return toil;
        }

        public static Toil RiskAnger(Pawn pawn, Pawn guest)
        {
            return new Toil
                   {
                       initAction = () => CheckAnger(pawn, guest),
                       defaultCompleteMode = ToilCompleteMode.Instant
                   };
        }

        private static void CheckAnger(Pawn recruiter, Pawn guest)
        {
            if (guest.Faction == null || recruiter == null || guest.Faction==Faction.OfPlayer) return;
            var map = recruiter.MapHeld;
            var allies = map.mapPawns.PawnsInFaction(guest.Faction).ToArray();
            foreach (var ally in allies)
            {
                if (ally != guest && !ally.Dead && ally.Spawned && ally.Awake() && ally.CanSee(recruiter) && ally.CanSee(guest))
                {
                    if (ally.needs.mood.thoughts.memories.Memories.Any(t=>t.def.defName=="GuestAngered")) continue;

                    float pleaseChance = recruiter.GetStatValue(statPleaseGuestChance);
                    pleaseChance = GuestUtility.AdjustPleaseChance(pleaseChance, recruiter, ally);
                    pleaseChance = Mathf.Clamp01(pleaseChance);

                    if (Rand.Value > pleaseChance)
                    {
                        //Log.Message("txtRecruitAngerOther");
                        Messages.Message(string.Format(txtRecruitAngerOther, recruiter.NameStringShort, guest.NameStringShort, pleaseChance.ToStringPercent(), ally.NameStringShort), MessageTypeDefOf.NegativeEvent);
						//ally.Faction.AffectGoodwillWith(Faction.OfColony, -1f + 0.045f * recruiter.skills.GetSkill(SkillDefOf.Social).level); //Skill based influence -0.1 ... -1
                        GuestUtility.GainSocialThought(recruiter, ally, ThoughtDef.Named("GuestAngered"));

                        //if (Rand.Value < GuestUtility.GetDismissiveChance(ally))
                        {
                            GuestUtility.GainSocialThought(recruiter, ally, ThoughtDef.Named("GuestDismissiveAttitude"));
                        }
                    }
                }
            }
        }

        protected override bool FailCondition()
        {
            return base.FailCondition() || !Talkee.TryRecruit();
        }
    }
}