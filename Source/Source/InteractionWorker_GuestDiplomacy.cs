using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Hospitality
{
    public class InteractionWorker_GuestDiplomacy : InteractionWorker
    {
        public override void Interacted(Pawn recruiter, Pawn guest, List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel, out LetterDef letterDef)
        {
            letterDef = null;
            letterLabel = null;
            letterText = null;
            if (recruiter == null || guest?.guest == null) return;

            GuestUtility.TryPleaseGuest(recruiter, guest, false, extraSentencePacks);
        }
    }
}