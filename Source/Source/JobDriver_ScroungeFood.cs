using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public class JobDriver_ScroungeFood : JobDriver
    {
        private Pawn OtherPawn => job.GetTarget(TargetIndex.A).Pawn;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnAggroMentalState(TargetIndex.A);
            this.FailOnIncapable(PawnCapacityDefOf.Manipulation);
            Toil toil = Toils_Interpersonal.GotoInteractablePosition(TargetIndex.A);
            toil.socialMode = RandomSocialMode.Off;
            yield return toil;
            Toil finalGoto = Toils_Interpersonal.GotoInteractablePosition(TargetIndex.A);
            yield return Toils_Jump.JumpIf(finalGoto, () => !OtherPawn.Awake());
            Toil toil2 = Toils_Interpersonal.WaitToBeAbleToInteract(pawn);
            toil2.socialMode = RandomSocialMode.Off;
            yield return toil2;
            finalGoto.socialMode = RandomSocialMode.Off;
            yield return finalGoto;
            yield return Toils_General.Do(delegate
            {
                if (!OtherPawn.Awake())
                {
                    OtherPawn.jobs.SuspendCurrentJob(JobCondition.InterruptForced);
                    var intDef = DefDatabase<InteractionDef>.GetNamed("ScroungeFoodAttempt");
                    if (!pawn.interactions.CanInteractNowWith(OtherPawn, intDef))
                    {
                        Log.Message($"{pawn.LabelCap} failed to scrounge food from {OtherPawn.Label}: Could not interact.");
                    }
                }
            });
            yield return Toils_Interpersonal.Interact(TargetIndex.A, DefDatabase<InteractionDef>.GetNamed("ScroungeFoodAttempt"));
        }
    }
}