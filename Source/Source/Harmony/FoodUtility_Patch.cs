using HarmonyLib;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
    internal static class FoodUtility_Patch
    {
        /// <summary>
        /// So guests will care
        /// </summary>
        [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.BestFoodSourceOnMap))]
        public class BestFoodSourceOnMap
        {
            [HarmonyPrefix]
            public static bool Prefix(Pawn eater, bool desperate)
            {
                if (!eater.IsArrivedGuest(out _)) return true;
                return desperate;
            }
        }
    }
}

    
