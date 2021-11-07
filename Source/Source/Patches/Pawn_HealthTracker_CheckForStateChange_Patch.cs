using HarmonyLib;
using RimWorld;
using Verse;

namespace Hospitality.Patches
{
    [HarmonyPatch(typeof(Pawn_HealthTracker), "CheckForStateChange", null)]
    public static class Pawn_HealthTracker_CheckForStateChange_Patch
    {
        private static void Postfix(Pawn_HealthTracker __instance, Pawn ___pawn, DamageInfo? dinfo, Hediff hediff)
        {
            if (___pawn.Map != null && !HealthAIUtility.ShouldSeekMedicalRest(___pawn) && !___pawn.health.hediffSet.HasNaturallyHealingInjury() && Utilities.GuestUtility.GuestHasNoLord(___pawn))
            {
                Utilities.GuestUtility.CheckForRoguePawn(___pawn, ___pawn.Map);
            }
        }
    }
}