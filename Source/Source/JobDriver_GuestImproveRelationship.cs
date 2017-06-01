using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public class JobDriver_GuestImproveRelationship : JobDriver_GuestBase
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(FailCondition);
            yield return GotoGuest(pawn, Talkee);
            yield return Toils_Reserve.Reserve(TargetIndex.A);
            //yield return GotoGuest(pawn, Talkee);
            yield return Interact(Talkee, InteractionDefOf.BuildRapport, GuestUtility.InteractIntervalAbsoluteMin);
            yield return TryImproveRelationship(pawn, Talkee);
            //yield return Toils_Interpersonal.SetLastInteractTime(TargetIndex.A);
        }

        public Toil TryImproveRelationship(Pawn recruiter, Pawn guest)
        {
            var toil = new Toil
            {
                initAction = () => {
                    if (!recruiter.ShouldImproveRelationship(guest)) return;
                    if (!recruiter.CanTalkTo(guest)) return;
                    InteractionDef intDef = DefDatabase<InteractionDef>.GetNamed("GuestDiplomacy"); 
                    recruiter.interactions.TryInteractWith(guest, intDef);
                },
                socialMode = RandomSocialMode.Off,
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = 350
            };
            toil.AddFailCondition(FailCondition);
            return toil;
        }

        protected override bool FailCondition()
        {
            return base.FailCondition() || !Talkee.ImproveRelationship();
        }
    }
}