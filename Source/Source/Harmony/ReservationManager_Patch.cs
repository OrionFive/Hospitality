using Harmony;
using Verse;
using Verse.AI;

namespace Hospitality.Harmony
{
    /// <summary>
    /// Suppresses "Could not reserve" error, caused by guests doing the job of a colonist. Doesn't seem to be a problem further.
    /// </summary>
    public class ReservationManager_Patch
    {
        [HarmonyPatch(typeof(ReservationManager), "LogCouldNotReserveError")]
        public class LogCouldNotReserveError
        {
            [HarmonyPrefix]
            public static bool Prefix(ReservationManager __instance, Pawn claimant, LocalTargetInfo target)
            {
                if (claimant.IsGuest()) return false;
                
                Pawn pawn = __instance.FirstRespectedReserver(target, claimant);
                if (pawn.IsGuest()) return false;

                return true;
            }
        }
    }
}