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
                    var silver = actor.inventory.innerContainer.FirstOrDefault(i => i.def == ThingDefOf.Silver);
                    var money = silver?.stackCount ?? 0;
                    
                    // Check the stored rentalFee (takeExtraIngestibles)... if it was increased, cancel!
                    if (!(TargetA.Thing is Building_GuestBed newBed) 
                        || newBed.rentalFee > job.takeExtraIngestibles 
                        || newBed.rentalFee > money) 
                    {
                        actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    var compGuest = actor.GetComp<CompGuest>();
                    if (compGuest.HasBed) Log.Error($"{actor.LabelShort} already has a bed ({compGuest.bed.Label})");

                    compGuest.ClaimBed(newBed);

                    if (newBed.rentalFee > 0)
                    {
                        actor.inventory.innerContainer.TryDrop(silver, actor.Position, Map, ThingPlaceMode.Near, newBed.rentalFee, out silver);
                    }
                }
            }.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        }
    }
}
