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
                if (!IsAcceptableForGuest(eater, __result, foodDef, desperate)) __result = null;
                Log.Message($"{eater.NameShortColored}: {foodDef.LabelCap} is acceptable == {IsAcceptableForGuest(eater, __result, foodDef, desperate)}");
            }
        }      
        
        [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.TryFindBestFoodSourceFor))]
        public class TryFindBestFoodSourceForPatch
        {
            [HarmonyPostfix]
            public static void Postfix(Pawn getter, Pawn eater, ref bool __result, ref Thing foodSource, ref ThingDef foodDef, ref bool desperate)
            {
                if (!eater.IsArrivedGuest(out _)) return;
                if (!IsAcceptableForGuest(eater, foodSource, foodDef, desperate)) __result = false;
            }
        }

        internal static bool IsAcceptableForGuest(Pawn guest, Thing foodSource, ThingDef foodDef, bool desperate)
        {
            if (foodSource == null)
            {
                return true;
            }

            if (desperate || guest.GetMapComponent().guestsCanTakeFoodForFree)
            {
                return true;
            }

            //If the food source is a gastronomy dining spot, allow to get food as well
            if (foodSource.def == DefOf.Gastronomy_DiningSpot && foodDef != null)
            {
                return true;
            }

            //Check whether the current food source is a dispenser set as a vending machine for this guest
            if (foodSource is Building_NutrientPasteDispenser dispenser && (dispenser.TryGetComp<CompVendingMachine>()?.CanBeUsedBy(guest, foodSource, foodDef) ?? false))
            {
                return true;
            }

            return false;
        }
    }
}

    
