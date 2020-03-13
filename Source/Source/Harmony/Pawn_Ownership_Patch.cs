using HarmonyLib;
using RimWorld;
using Verse;

namespace Hospitality.Harmony {
    /// <summary>
    /// So guests will properly unclaim their beds.
    /// </summary>
    internal static class Pawn_Ownership_Patch
    {
        [HarmonyPatch(typeof(Pawn_Ownership), "UnclaimBed")]
        public class UnclaimBed
        {
            [HarmonyPrefix]
            public static bool Prefix(Pawn ___pawn)
            {
                ___pawn.GetComp<CompGuest>()?.ClearOwnership();
                return true;
            }
        }
    }
}