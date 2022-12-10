using HarmonyLib;
using Hospitality.Utilities;
using Verse;

namespace Hospitality.Patches;

/// <summary>
/// So guests don't birth children during their visit
/// </summary>
public class Hediff_Pregnant_Patch
{
    [HarmonyPatch(typeof(Hediff_Pregnant), nameof(Hediff_Pregnant.Tick))]
    public class Tick
    {
        [HarmonyPrefix]
        public static bool Postfix(ref Hediff_Pregnant __instance)
        {
            return __instance?.pawn?.IsGuest() != true;
        }
    }
}