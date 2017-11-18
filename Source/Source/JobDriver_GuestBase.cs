using RimWorld;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public abstract class JobDriver_GuestBase : JobDriver_ChatWithPrisoner
    {
        public static readonly JobDef therapyJobDef = DefDatabase<JobDef>.GetNamedSilentFail("ReceiveTherapy");

        public static Toil GotoGuest(Pawn pawn, Pawn talkee, bool mayBeSleeping = false)
        {
            var toil = new Toil
            {
                initAction = () => pawn.pather.StartPath(talkee, PathEndMode.Touch),
                defaultCompleteMode = ToilCompleteMode.PatherArrival
            };
            toil.AddFailCondition(() => !GuestUtility.ViableGuestTarget(talkee, mayBeSleeping));
            return toil;
        }

        protected Toil Interact(Pawn talkee, InteractionDef intDef, int duration)
        {
            var toil = new Toil {
                initAction = () => {
                    if (talkee.interactions.InteractedTooRecentlyToInteract()
                        || pawn.interactions.InteractedTooRecentlyToInteract()) return;

                    PawnUtility.ForceWait(talkee, duration, pawn);
                    TargetThingB = pawn;
                    MoteMaker.MakeInteractionBubble(pawn, talkee, intDef.interactionMote, intDef.Symbol);
                }, 
                socialMode = RandomSocialMode.Normal,
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration =  duration
            };
            return toil.WithProgressBarToilDelay(TargetIndex.B);
        }

        protected virtual bool FailCondition()
        {
            return !GuestUtility.ViableGuestTarget(Talkee) || (!pawn.HasReserved(Talkee) && !pawn.CanReserve(Talkee));
        }

        protected static bool IsBusy(Pawn p)
        {
            return p.interactions.InteractedTooRecentlyToInteract() || p.Map.reservationManager.IsReservedByAnyoneOf(p, Faction.OfPlayer) || IsInTherapy(p);
        }

        // Compatibility fix to Therapy mod
        private static bool IsInTherapy(Pawn p)
        {
            return therapyJobDef != null && p.CurJob != null && p.CurJob.def == therapyJobDef;
        }
    }
}