using Harmony;
using RimWorld;
using Verse;
using Verse.AI;

namespace Hospitality.Harmony {
    /// <summary>
    /// Make sure things outside the guest zone are not reachable for guests
    /// </summary>
    public class Reachability_Patch
    {
        [HarmonyPatch(typeof(Reachability), "CanReach", typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms))]
        public class CanReach
        {
            [HarmonyPostfix]
            public static void Postfix(ref bool __result, ref Reachability __instance, LocalTargetInfo dest, PathEndMode peMode, TraverseParms traverseParams)
            {
                if (!__result) return;

                if (!traverseParams.pawn.IsGuest()) return;
                if (!traverseParams.pawn.IsArrived()) return;

                var area = traverseParams.pawn.GetGuestArea();
                if (area == null) return;
                if (!dest.IsValid || !area[dest.Cell]) __result = false;

                //Log.Message($"Guest {traverseParams.pawn.LabelShort} tried to traverse to {dest.Cell}. This was {(__result ? "allowed" : "not allowed")}");
            }
        }
    }
}