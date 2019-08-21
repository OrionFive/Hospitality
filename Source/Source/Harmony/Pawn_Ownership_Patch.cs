using Harmony;
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
            public static bool Prefix(Pawn_Ownership __instance)
            {
                var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
                
                //Log.Message($"Trying to Unclaim Bed of {pawn.LabelShort}... OwnedBed = {__instance.OwnedBed?.Label}");
                
                pawn.GetComp<CompGuest>()?.ClearOwnership();
                return true;
            }
        }
    }
}