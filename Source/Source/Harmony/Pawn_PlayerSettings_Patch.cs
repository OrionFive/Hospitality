using Harmony;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
    public static class Pawn_PlayerSettings_Patch
    {
        // Added so guests will respect their assigned area
        [HarmonyPatch(typeof(Pawn_PlayerSettings))]
        [HarmonyPatch("RespectsAllowedArea", PropertyMethod.Getter)]
        public static class RespectsAllowedArea
        {
            [HarmonyPostfix]
            public static void Postfix(Pawn_PlayerSettings __instance, ref bool __result)
            {
                var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
                __result |= pawn.IsGuest();
            }
        }
    }
}