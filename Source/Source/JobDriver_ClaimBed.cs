using System.Collections.Generic;
using System.Linq;
using Hospitality.Utilities;
using RimWorld;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public class JobDriver_ClaimBed : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (TargetA.Thing is not Building_GuestBed newBed) return false;
            return pawn.Reserve(TargetA, job, newBed.SleepingSlotsCount, 0, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.EndOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOn(_ => BedCantBeClaimedAnymore());//.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            yield return ClaimBed();
        }

        private bool BedCantBeClaimedAnymore()
        {
            var bed = TargetA.Thing as Building_GuestBed;
            if (bed == null) return true;
            if (bed.IsForbidden(pawn)) return false;

            var result = !bed.AnyUnownedSleepingSlot || bed.CompAssignableToPawn.IdeoligionForbids(pawn);
            //if (result) Log.Message($"{pawn.LabelShort} failed to claim {TargetA.Thing.LabelShort}. Ideology forbids: {bed.CompAssignableToPawn.IdeoligionForbids(pawn)}");
            return result;
        }

        private Toil ClaimBed()
        {
            return new Toil
            {
                initAction = () => {
                    var actor = GetActor();
                    var silver = actor.inventory.innerContainer.FirstOrDefault(i => i.def == ThingDefOf.Silver);
                    var money = silver?.stackCount ?? 0;
                    
                    // Check the stored RentalFee (takeExtraIngestibles)... if it was increased, cancel!
                    if (TargetA.Thing is not Building_GuestBed newBed 
                        || newBed.RentalFee > job.takeExtraIngestibles 
                        || newBed.RentalFee > money) 
                    {
                        actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    if (BedCantBeClaimedAnymore())
                    {
                        actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    var compGuest = actor.CompGuest();
                    if (compGuest.HasBed)
                    {
                        Log.Error($"{actor.LabelShort} already has a bed ({compGuest.bed.Label})");
                        return;
                    }

                    compGuest.ClaimBed(newBed);

                    if (newBed.RentalFee > 0)
                    {
                        actor.inventory.innerContainer.TryDrop(silver, actor.Position, Map, ThingPlaceMode.Near, newBed.RentalFee, out silver);
                    }
                    actor.ThoughtAboutClaimedBed(newBed, money);
                }
            }.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        }
    }
}
