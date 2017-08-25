using System;
using Harmony;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
    public class MinifiedThing_Patch
    {
        [HarmonyPatch(typeof(MinifiedThing), "DrawAt")]
        public class DrawAt
        {
            public static bool Prefix(MinifiedThing __instance)
            {
                if (!__instance.def.Minifiable)
                {
                    // Destroy next tick
                    ModBaseHospitality.RegisterTickAction(DestroySafely(__instance));
                    return false;
                }
                return true;
            }

            private static Action DestroySafely(MinifiedThing __instance)
            {
                return () => {
                    try
                    {
                        Log.Message("Removing invalid minified thing of type "+__instance.def.LabelCap+".");
                        __instance.Destroy();
                    }
                    catch (NullReferenceException)
                    {
                        // We're expecting one from the InstallBlueprintUtility - can't be avoided
                    }
                };
            }
        }
    }
}