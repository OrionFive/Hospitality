using Harmony;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
    /// <summary>
    /// Added Joy and Comfort to guests
    /// </summary>
    internal static class Pawn_NeedsTracker_Patch
    {
        [HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
        public class ShouldHaveNeed
        {
            private static readonly NeedDef defComfort = DefDatabase<NeedDef>.GetNamed("Comfort");
            private static readonly NeedDef defBeauty = DefDatabase<NeedDef>.GetNamed("Beauty");
            private static readonly NeedDef defSpace = DefDatabase<NeedDef>.GetNamed("RoomSize");

            [HarmonyPrefix]
            public static bool Prefix(Pawn_NeedsTracker __instance, ref bool __result, NeedDef nd)
            {
                var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();

                if ((nd == NeedDefOf.Joy || nd == defComfort || nd == defBeauty || nd == defSpace) && pawn.IsGuest()) // ADDED
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }
    }
}