using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Hospitality
{
    public class InteractionWorker_GuestDiplomacy : InteractionWorker
    {
        public override void Interacted(Pawn recruiter, Pawn guest, List<RulePackDef> extraSentencePacks)
        {
            if (recruiter == null || guest == null || guest.guest == null) return;

            GuestUtility.TryPleaseGuest(recruiter, guest, false);
        }
    }
}