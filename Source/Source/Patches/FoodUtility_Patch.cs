using HarmonyLib;
using RimWorld;
using Verse;

namespace Hospitality.Patches
{
    internal static class FoodUtility_Patch
    {
        /// <summary>
        /// So guests will care
        /// </summary>
        [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.BestFoodSourceOnMap))]
        public class BestFoodSourceOnMapPatch
        {
            [HarmonyPostfix]
            public static void Postfix(Pawn getter, Pawn eater, bool desperate, ThingDef foodDef, ref Thing __result)
            {
                if (!eater.IsArrivedGuest(out _)) return;
                if (!Utilities.FoodUtility.GuestCanUseFoodSource(eater, __result, foodDef, desperate)) __result = null;
                Log.Message($"{eater.NameShortColored}: {foodDef.LabelCap} is acceptable == {Utilities.FoodUtility.GuestCanUseFoodSource(eater, __result, foodDef, desperate)}");
            }
        }
        
        [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.TryFindBestFoodSourceFor))]
        public class TryFindBestFoodSourceForPatch
        {
            [HarmonyPostfix]
            public static void Postfix(Pawn getter, Pawn eater, ref bool __result, ref Thing foodSource, ref ThingDef foodDef, ref bool desperate)
            {
                if (!eater.IsArrivedGuest(out _)) return;
                if (!Utilities.FoodUtility.GuestCanUseFoodSource(eater, foodSource, foodDef, desperate)) __result = false;
            }
        }
    }
}

    
