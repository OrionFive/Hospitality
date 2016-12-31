using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Hospitality
{
    public class InteractionWorker_GuestDiplomacy : InteractionWorker
    {
        private static readonly StatDef statPleaseGuestChance = StatDef.Named("PleaseGuestChance");

        public override void Interacted(Pawn recruiter, Pawn guest, List<RulePackDef> extraSentencePacks)
        {
            if (recruiter == null || guest == null || guest.guest == null) return;

            // TODO: pawn.records.Increment(RecordDefOf.GuestsChatted);
            //recruiter.skills.Learn(SkillDefOf.Social, 25f);
            float pleaseChance = recruiter.GetStatValue(statPleaseGuestChance);
            pleaseChance = GuestUtility.AdjustPleaseChance(pleaseChance, recruiter, guest);
            pleaseChance = Mathf.Clamp01(pleaseChance);

            if (Rand.Value > pleaseChance)
            {
                Messages.Message(
                    "ImproveFactionAnger".Translate(new object[]
                    {recruiter.NameStringShort, guest.NameStringShort, guest.Faction.Name, (1 - pleaseChance).ToStringPercent()}), guest,
                    MessageSound.Negative);

                GuestUtility.GainSocialThought(recruiter, guest, ThoughtDef.Named("GuestOffendedRelationship"));
            }
            else
            {
                Messages.Message(
                    "ImproveFactionPlease".Translate(new object[]
                    {recruiter.NameStringShort, guest.NameStringShort, guest.Faction.Name, (pleaseChance).ToStringPercent()}), guest,
                    MessageSound.Benefit);

                GuestUtility.GainSocialThought(recruiter, guest, ThoughtDef.Named("GuestPleasedRelationship"));
            }

            //if (Rand.Value < GuestUtility.GetDismissiveChance(guest))

            GuestUtility.GainSocialThought(recruiter, guest, ThoughtDef.Named("GuestDismissiveAttitude"));
        }
    }
}