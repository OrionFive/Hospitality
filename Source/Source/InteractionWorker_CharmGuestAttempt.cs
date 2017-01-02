using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Hospitality
{
    public class InteractionWorker_CharmGuestAttempt : InteractionWorker
    {
        private static readonly StatDef statPleaseGuestChance = StatDef.Named("PleaseGuestChance");
        private static readonly StatDef statRecruitEffectivity = StatDef.Named("RecruitEffectivity");

        public override void Interacted(Pawn recruiter, Pawn guest, List<RulePackDef> extraSentencePacks)
        {
            if (recruiter == null || guest == null || guest.guest == null) return;

            // TODO: pawn.records.Increment(RecordDefOf.GuestsCharmAttempts);
            //recruiter.skills.Learn(SkillDefOf.Social, 35f);
            float pleaseChance = recruiter.GetStatValue(statPleaseGuestChance);
            pleaseChance = GuestUtility.AdjustPleaseChance(pleaseChance, recruiter, guest);
            pleaseChance = Mathf.Clamp01(pleaseChance);

            if (Rand.Value > pleaseChance)
            {
                var isAbrasive = recruiter.story.traits.HasTrait(TraitDefOf.Abrasive);
                int multiplier = isAbrasive ? 2 : 1;
                string multiplierText = multiplier > 1 ? " x" + multiplier : string.Empty;

                string textAnger = recruiter.gender == Gender.Female ? "RecruitAngerSelfF" : "RecruitAngerSelfM";
                Messages.Message(
                    textAnger.Translate(new object[]
                    {recruiter.NameStringShort, guest.NameStringShort, (1-pleaseChance).ToStringPercent(), multiplierText}),
                    guest, MessageSound.Negative);

                extraSentencePacks.Add(RulePackDef.Named("Sentence_CharmAttemptRejected"));
                for (int i = 0; i < multiplier; i++)
                {
                    GuestUtility.GainSocialThought(recruiter, guest, ThoughtDef.Named("GuestOffended"));
                }
            }
            else
            {
                var statValue = recruiter.GetStatValue(statRecruitEffectivity);
                var floor = Mathf.FloorToInt(statValue);
                int multiplier = floor + (Rand.Value < statValue - floor ? 1 : 0);
                string multiplierText = multiplier > 1 ? " x" + multiplier : string.Empty;

                string textPlease = recruiter.gender == Gender.Female ? "RecruitPleaseSelfF" : "RecruitPleaseSelfM";
                Messages.Message(
                    textPlease.Translate(new object[]
                    {recruiter.NameStringShort, guest.NameStringShort, (pleaseChance).ToStringPercent(), multiplierText}),
                    guest, MessageSound.Benefit);
                for (int i = 0; i < multiplier; i++)
                {
                    GuestUtility.GainSocialThought(recruiter, guest, ThoughtDef.Named("GuestConvinced"));
                }
                extraSentencePacks.Add(RulePackDef.Named("Sentence_CharmAttemptAccepted"));
            }
            GuestUtility.GainSocialThought(recruiter, guest, ThoughtDef.Named("GuestDismissiveAttitude"));
        }
    }
}
