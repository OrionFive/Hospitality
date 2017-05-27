using Harmony;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
    public static class Pawn_RelationsTracker_Patch
    {
        /// <summary>
        /// Rescued pawns get marked as rescued for follow up
        /// </summary>
        [HarmonyPatch(typeof(Pawn_RelationsTracker), "Notify_RescuedBy")]
        public static class Notify_RescuedBy
        {
            [HarmonyPostfix]
            public static void Postfix(Pawn_RelationsTracker __instance)
            {
                var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
                var compGuest = pawn.GetComp<CompGuest>();
                if (compGuest != null)
                {
                    compGuest.OnRescued();
                }
            }
        }
    }
}

}
