using Harmony;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
    internal static class ForbidUtility_Patch
    {
        // So guests will care
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
    }
}