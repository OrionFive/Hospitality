using System.Linq;
using Harmony;
using Verse;

namespace Hospitality.Harmony
{
    /// <summary>
    /// Make filters for beds also accept guest beds
    /// </summary>
    public class ThingFilter_Patch
    {
        [HarmonyPatch(typeof(ThingFilter), "Allows", typeof(ThingDef))]
        public class Allows
        {
            [HarmonyPrefix]
            public static bool Prefix(ref ThingFilter __instance, ref bool __result, ThingDef def)
            {
                if (def.thingClass == typeof(Building_GuestBed))
                {
                    var bedDef = DefDatabase<ThingDef>.GetNamed(def.defName.Substring(0, def.defName.Length - 5)); // remove "Guest" from name

                    __result = __instance.AllowedThingDefs.Contains(bedDef);
                    return false;
                }

                // Business as usual
                return true;
            }
        }
    }
}
