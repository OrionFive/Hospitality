using HarmonyLib;
using RimWorld;
using Verse;

namespace Hospitality.Patches
{
    /// <summary>
    /// So guests will care
    /// </summary>
    internal static class FoodUtility_Patch
    {
        [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.IsFoodSourceOnMapSociallyProper))]
        public class IsFoodSourceOnMapSociallyProperPatch
        {
            public static void Postfix(Thing t, Pawn getter, Pawn eater, bool allowSociallyImproper, ref bool __result)
            {
                if (!eater.IsArrivedGuest(out _)) return;
                __result = Utilities.FoodUtility.GuestCanUseFoodSourceInternal(eater, t);
            }
        }

        [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.BestFoodSourceOnMap))]
        public class BestFoodSourceOnMapPatch
        {
            [HarmonyPostfix]
            public static void Postfix(Pawn getter, Pawn eater, bool desperate, ThingDef foodDef, ref Thing __result)
            {
                if (!eater.IsArrivedGuest(out _)) return;
                if (Utilities.FoodUtility.GuestCanUseFoodSourceExceptions(eater, __result, foodDef, desperate)) return;
                __result = null;
            }
        }
        
        [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.TryFindBestFoodSourceFor))]
        public class TryFindBestFoodSourceForPatch
        {
            [HarmonyPostfix]
            public static void Postfix(Pawn getter, Pawn eater, ref bool __result, ref Thing foodSource, ref ThingDef foodDef, ref bool desperate)
            {
                if (!eater.IsArrivedGuest(out _)) return;
                if (Utilities.FoodUtility.GuestCanUseFoodSourceExceptions(eater, foodSource, foodDef, desperate)) return;
                __result = false;
            }
        }
    }
}


    
