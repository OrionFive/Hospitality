using Harmony;
using RimWorld;
using Verse;
using Verse.AI;

namespace Hospitality.Harmony
{
    internal static class ForbidUtility_Patch
    {
        /// <summary>
        /// So guests will care
        /// </summary>
        [HarmonyPatch(typeof(ForbidUtility), "CaresAboutForbidden")]
        public class CaresAboutForbidden
        {
            [HarmonyPrefix]
            public static bool Replacement(ref bool __result, Pawn pawn, bool cellTarget)
            {
                __result = !pawn.InMentalState && AddedFactionCheck(pawn)
                       && (!cellTarget || !ThinkNode_ConditionalShouldFollowMaster.ShouldFollowMaster(pawn));
                return false;
            }

            private static bool AddedFactionCheck(Pawn pawn)
            {
                return pawn.HostFaction == null || pawn.IsGuest();
            }
        }

        /// <summary>
        /// Set by JobDriver_Patch and stores who is doing a toil right now, in which case we don't want to forbid things.
        /// </summary>
        public static Pawn currentToilWorker;

        /// <summary>
        /// Things dropped by guests are never forbidden
        /// </summary>
        [HarmonyPatch(typeof(ForbidUtility), "SetForbidden")]
        public class SetForbidden
        {
            [HarmonyPrefix]
            public static bool Prefix(Thing t, bool value)
            {
                if (value && currentToilWorker.IsGuest())
                {
                    return false;
                }
                return true;
            }
        }
    }
}