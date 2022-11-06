using System.Collections.Generic;
using Hospitality.Utilities;
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
            Toil toil = GotoPawn(TargetIndex.A);
            toil.socialMode = RandomSocialMode.Off;
            yield return toil;
            Toil finalGoto = GotoPawn(TargetIndex.A);
            yield return Toils_Jump.JumpIf(finalGoto, () => !OtherPawn.Awake());
            Toil toil2 = Toils_Interpersonal.WaitToBeAbleToInteract(pawn);
            toil2.socialMode = RandomSocialMode.Off;
            yield return toil2;
            finalGoto.socialMode = RandomSocialMode.Off;
            yield return finalGoto;
            yield return AskForFood(TargetIndex.A, TargetIndex.B);
            yield return ItemUtility.TakeFromPawn(job.targetB.Thing, job.targetA.Pawn.inventory.innerContainer, job.count, TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_General.Wait(80);
            yield return ItemUtility.TakeToInventory(TargetIndex.B);

        }

        public static Toil GotoPawn(TargetIndex targetInd)
        {
            var toil = ToilMaker.MakeToil();
            toil.tickAction = delegate
            {
                var actor = toil.actor;
                var target = actor.jobs.curJob.GetTarget(targetInd);

                if (target != actor.pather.Destination || (!actor.pather.Moving && !actor.CanReachImmediate(target, PathEndMode.Touch)))
                {
                    actor.pather.StartPath(target, PathEndMode.Touch);
                }
                else if (actor.CanReachImmediate(target, PathEndMode.Touch))
                {
                    actor.jobs.curDriver.ReadyForNextToil();
                }
            };
            toil.socialMode = RandomSocialMode.Off;
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            return toil;
        }

        public static Toil AskForFood(TargetIndex targetInd, TargetIndex foodInd)
        {
            var toil = ToilMaker.MakeToil("Scrounge food interaction"); ;
            toil.defaultDuration = 100;
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.socialMode = RandomSocialMode.Off;
            toil.activeSkill = () => SkillDefOf.Social;
            toil.tickAction = TickAction;
            toil.initAction = InitAction;
            return toil;

            void InitAction()
            {
                Pawn actor = toil.actor;
                Job curJob = actor.CurJob;
                LocalTargetInfo target = curJob.GetTarget(targetInd);
                LocalTargetInfo targetFood = curJob.GetTarget(foodInd);

                var targetPawn = target.Pawn;
                if (!target.HasThing || targetPawn == null)
                {
                    Log.Warning($"Can't scrounge, no target.");
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                var food = targetFood.Thing;
                if (!targetFood.HasThing || food == null)
                {
                    Log.Warning($"Can't scrounge, no food.");
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                var symbol = food.def.uiIcon;
                if (symbol != null) GenericUtility.TryCreateBubble(actor, targetPawn, symbol);
                var intDef = DefDatabase<InteractionDef>.GetNamed("ScroungeFoodAttempt");
                actor.interactions.lastInteractionTime = Find.TickManager.TicksGame;
                actor.interactions.lastInteraction = intDef.defName;
                var list = new List<RulePackDef>();
                intDef.Worker.Interacted(actor, targetPawn, list, out _, out _, out _, out _);
                Find.PlayLog.Add(new PlayLogEntry_Interaction(intDef, actor, targetPawn, list));
            }

            void TickAction()
            {
                toil.actor.rotationTracker.FaceCell(toil.actor.CurJob.GetTarget(targetInd).Cell);
            }
        }
    }
}