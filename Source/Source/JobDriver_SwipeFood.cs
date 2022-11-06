using System.Collections.Generic;
using RimWorld;
using Verse.AI;

namespace Hospitality
{
    public class JobDriver_SwipeFood : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnAggroMentalState(TargetIndex.A);
            this.FailOnCannotTouch(TargetIndex.A, PathEndMode.ClosestTouch);
            this.FailOnIncapable(PawnCapacityDefOf.Manipulation);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).socialMode = RandomSocialMode.Off;
            yield return ItemUtility.TakeFromPawn(job.targetB.Thing, job.targetA.Pawn.inventory.innerContainer, job.count, TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_General.Wait(150);
            yield return ItemUtility.TakeToInventory(TargetIndex.B);
        }
    }
}