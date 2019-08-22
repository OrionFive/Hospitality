using Harmony;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
    /// <summary>
    /// Mark rescued guests for reward
    /// </summary>
    internal static class Pawn_RelationsTracker_Patch
    {
        [HarmonyPatch(typeof(Pawn_RelationsTracker), "Notify_RescuedBy")]
        public class Notify_RescuedBy
        {
            [HarmonyPrefix]
            public static void Prefix(Pawn_NeedsTracker __instance)
            {
                var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();

                if (pawn.Faction == Faction.OfPlayer) return;

                var compGuest = pawn.GetComp<CompGuest>();
                if (compGuest != null) compGuest.rescued = true;
            }
        }
    }
}
