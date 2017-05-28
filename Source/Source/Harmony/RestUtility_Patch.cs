using Harmony;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
    /// <summary>
    /// Allow only visitors to use only guest beds
    /// </summary>
    internal static class RestUtility_Patch
    {
        [HarmonyPatch(typeof(RestUtility), "CanUseBedEver")]
        public class CanUseBedEver
        {
            [HarmonyPostfix]
            public static void Postfix(Pawn p, ThingDef bedDef, ref bool __result)
            {
                if (bedDef.thingClass == typeof(Building_GuestBed))
                {
                    __result &= p.IsGuest();
                }
            }
        }
    }
}