using Harmony;
using RimWorld;
using Verse.AI;

namespace Hospitality.Harmony
{
    public class JobDriver_Ingest_Patch
    {
        [HarmonyPatch(typeof(JobDriver_Ingest), "ReserveFoodIfWillIngestWholeStack")]
        public class ReserveFoodIfWillIngestWholeStack
        {
            [HarmonyPostfix]
            public static void Postfix(ref Toil __result)
            {
                __result = __result.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            }
        }
    }
}