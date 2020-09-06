using HarmonyLib;
using Verse;
using Verse.AI;

namespace Hospitality.Harmony
{
    /// <summary>
    /// Works together with ForbidUtility_Patch to prevent guests from forbidding items during work
    /// </summary>
    public class JobDriver_Patch
    {
        [HarmonyPatch(typeof(JobDriver), nameof(JobDriver.DriverTick))]
        public class DriverTick
        {
            [HarmonyPrefix]
            public static void Prefix(Pawn ___pawn)
            {
                ForbidUtility_Patch.currentToilWorker = ___pawn;
            }
        }
    }
}