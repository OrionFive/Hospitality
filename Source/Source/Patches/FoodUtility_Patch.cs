using System;
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

        /// <summary>
        /// Patching _NewTemp if it exists, or original version if it doesn't, so players with older versions don't run into issues.
        /// Also: Goddammit, Ludeon :(
        /// </summary>
        //[HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.TryFindBestFoodSourceFor))]
        public class TryFindBestFoodSourceFor_Patch
        {
            [PatchManually]
            public static void PatchManually(Harmony harmony)
            {
                var method = AccessTools.Method(typeof(FoodUtility), nameof(FoodUtility.TryFindBestFoodSourceFor_NewTemp));
                if(method == null) Log.Message($"Hospitality: TryFindBestFoodSourceFor_NewTemp not found, patching original method.");
                method ??= AccessTools.Method(typeof(FoodUtility), nameof(FoodUtility.TryFindBestFoodSourceFor));

                var postfix = new HarmonyMethod(AccessTools.Method(typeof(TryFindBestFoodSourceFor_Patch), "Postfix"));
                harmony.Patch(method, postfix: postfix);
            }

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


    
