using HarmonyLib;
using RimWorld;
using Verse;
using GuestUtility = Hospitality.Utilities.GuestUtility;

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
                if (!GuestUtility.IsArrivedGuest(eater, out _)) return;
                __result = Utilities.FoodUtility.GuestCanUseFoodSourceInternal(eater, t);
            }
        }

        [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.BestFoodSourceOnMap))]
        public class BestFoodSourceOnMapPatch
        {
            [HarmonyPostfix]
            public static void Postfix(Pawn getter, Pawn eater, bool desperate, ThingDef foodDef, ref Thing __result)
            {
                if (!GuestUtility.IsArrivedGuest(eater, out _)) return;
                if (Utilities.FoodUtility.GuestCanUseFoodSourceExceptions(eater, __result, foodDef, desperate)) return;
                __result = null;
            }
        }

        [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.FoodOptimality))]
        public class FoodOptimalityPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref float __result, Pawn eater, Thing foodSource, ThingDef foodDef, float dist, bool takingToInventory = false)
            {
                if (GuestUtility.IsGuest(eater) && foodSource is Building_NutrientPasteDispenser nutrientPasteDispenser)
                {
                    var comp = foodSource.TryGetComp<CompVendingMachine>();
                    if (comp != null && comp.IsActive())
                    {
                        Log.Message($"Before: FoodOptimality for {eater}, price: {comp.CurrentPrice}, base market value: {nutrientPasteDispenser.DispensableDef.BaseMarketValue}, result: {__result}");
                        __result *= nutrientPasteDispenser.DispensableDef.BaseMarketValue / comp.CurrentPrice;
                        Log.Message($"After: FoodOptimality for {eater}, price: {comp.CurrentPrice}, base market value: {nutrientPasteDispenser.DispensableDef.BaseMarketValue}, result: {__result}");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.TryFindBestFoodSourceFor_NewTemp))]
        public class TryFindBestFoodSourceForPatch
        {
            [HarmonyPostfix]
            public static void Postfix(Pawn getter, Pawn eater, ref bool __result, ref Thing foodSource, ref ThingDef foodDef, ref bool desperate)
            {
                if (!GuestUtility.IsArrivedGuest(eater, out _)) return;
                if (Utilities.FoodUtility.GuestCanUseFoodSourceExceptions(eater, foodSource, foodDef, desperate)) return;
                __result = false;
            }
        }
    }
}


    
