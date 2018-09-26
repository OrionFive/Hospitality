using Harmony;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
    /// <summary>
    /// Sometimes receives null for pawn...
    /// </summary>
    public class Building_Door_Patch
    {
        [HarmonyPatch(typeof(Building_Door), "PawnCanOpen")]
        public class PawnCanOpen
        {
            [HarmonyPrefix]
            public static bool Prefix(ref bool __result, Pawn p)
            {
                __result = false;

                if (p == null) return false;
                return true;
            }
        }
    }
}