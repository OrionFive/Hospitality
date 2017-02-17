using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Hospitality
{
    public class InteractionWorker_CharmGuestAttempt : InteractionWorker
    {
        public override void Interacted(Pawn recruiter, Pawn guest, List<RulePackDef> extraSentencePacks)
        {
            guest.CheckRecruitingSuccessful(recruiter, extraSentencePacks);
        }
    }
}
