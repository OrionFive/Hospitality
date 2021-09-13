using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public class JobDriver_EmptyVendingMachine : JobDriver
    {
        private const TargetIndex IndexRegister = TargetIndex.A;
        private const TargetIndex IndexSilver = TargetIndex.B;
        private CompVendingMachine VendingMachine => job.GetTarget(IndexRegister).Thing.TryGetComp<CompVendingMachine>();
        private Thing Silver => job.GetTarget(IndexSilver).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(VendingMachine.parent, job, 1, 1, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(IndexRegister);
            this.FailOnForbidden(IndexRegister);
            yield return Toils_Goto.GotoThing(IndexRegister, PathEndMode.Touch);
            yield return Toils_General.Do(GetSilver);
            //yield return Toils_Haul.StartCarryThing(IndexSilver, true).FailOnDestroyedOrNull(IndexSilver);
            yield return Toils_General.DoAtomic(Haul).FailOnDestroyedOrNull(IndexSilver);
        }

        private void Haul()
        {
            if (HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, Silver, false))
            {
                var haulJob = HaulAIUtility.HaulToStorageJob(pawn, Silver);
                if (haulJob != null)
                {
                    pawn.jobs.StartJob(haulJob, JobCondition.Succeeded);
                    return;
                }
            }
            pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
        }

        private void GetSilver()
        {
            //TryDrop(Silver, ThingPlaceMode.Near, out _, (thing, i) => pawn.CurJob.SetTarget(IndexSilver, thing))
            VendingMachine.GetDirectlyHeldThings().TryDrop(Silver, VendingMachine.parent.Position, VendingMachine.parent.Map, ThingPlaceMode.Near, Silver.stackCount, out _, (thing, i) => pawn.CurJob.SetTarget(IndexSilver, thing));
        }
	}
}
