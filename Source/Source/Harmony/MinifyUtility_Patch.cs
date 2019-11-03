using Harmony;
using RimWorld;
using Verse;

namespace Hospitality.Harmony {
    /// <summary>
    /// Turn guest bed into regular bed when minifying
    /// </summary>
    public class MinifyUtility_Patch
    {
        [HarmonyPatch(typeof (MinifyUtility), "MakeMinified")]
        public class MakeMinified
        {
            [HarmonyPrefix]
            public static void Prefix(ref Thing thing)
            {
                if (thing is Building_GuestBed bed && thing.Spawned)
                {
                    thing = Building_GuestBed.Swap(bed);
                }
            }
        }
    }
}
