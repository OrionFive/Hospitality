using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public class JobDriver_ClaimBed : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);//.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            yield return GetClaimToil();
        }

        private Toil GetClaimToil()
        {
            return new Toil
            {
                initAction = () => {
                    var actor = GetActor();
                    if (!(TargetA.Thing is Building_GuestBed newBed))
                    {
                        actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    var compGuest = actor.GetComp<CompGuest>();
                    if (compGuest.HasBed) Log.Error($"{actor.LabelShort} already has a bed ({compGuest.bed.Label})");

                    compGuest.ClaimBed(newBed);
                }
            }.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        }
    }
}
