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
            if (pawn.Reserve(TargetA, job, newBed.SleepingSlotsCount, 0, null, errorOnFailed)) return true;

            Log.Message($"{pawn.LabelShort} failed to reserve {TargetA.Thing.LabelShort} at {TargetA.Thing.Position}");
            return false;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.EndOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOn(BedHasBeenClaimed);//.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            yield return ClaimBed();
        }

        private bool BedHasBeenClaimed(Toil toil)
        {
            var bed = TargetA.Thing as Building_GuestBed;
            var claimed = bed is not { AnyUnownedSleepingSlot: true};
            if (claimed) Log.Message($"{pawn.LabelShort} failed to claim {TargetA.Thing?.LabelShort}. It has no remaining sleeping slots (current owners = {bed?.OwnersForReading.Select(o => o?.LabelShort).ToCommaList(emptyIfNone: true)})");
            return claimed;
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

                    if (!newBed.AnyUnownedSleepingSlot)
                    {
                        Log.Message($"{actor.LabelShort} failed to claim {newBed.LabelShort} - no sleeping slot available!");
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
