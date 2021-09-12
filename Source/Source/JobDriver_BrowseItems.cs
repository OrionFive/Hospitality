using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public class JobDriver_BrowseItems : JobDriver
    {
        private int ticksLeft;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft");
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            //yield return Toils_Reserve.Reserve(TargetIndex.A);
            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.Touch);
            var toil = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Never,
                initAction = delegate { ticksLeft = Rand.Range(GenDate.TicksPerHour / 2, GenDate.TicksPerHour); },
                tickAction = delegate {
                    pawn.rotationTracker.FaceCell(job.GetTarget(TargetIndex.B).Cell);
                    pawn.GainComfortFromCellIfPossible();
                }
            };
            toil.AddPreInitAction(delegate {
                var minDuration = ticksLeft / 2;
                ticksLeft--;
                if (ticksLeft <= 0)
                {
                    ReadyForNextToil();
                }
                else if (ticksLeft < minDuration && pawn.IsHashIntervalTick(100))
                {
                    pawn.jobs.CheckForJobOverride();
                }
            });
            yield return toil;
        }
    }
}