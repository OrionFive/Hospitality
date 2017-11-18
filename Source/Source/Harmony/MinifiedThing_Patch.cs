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
                if (!(__instance.InnerThing is Building_Bed)) return true;
                if (__instance.InnerThing == null 
                 || __instance.InnerThing.def == null 
                 || !__instance.InnerThing.def.Minifiable)
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
                        if (__instance != null && __instance.InnerThing != null && __instance.InnerThing.def != null)
                        {
                            Log.Message("Removing invalid minified thing of type "+__instance.InnerThing.def.LabelCap+".");
                        }
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