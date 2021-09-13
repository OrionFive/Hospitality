using RimWorld;
using Verse;

namespace Hospitality.Utilities
{
    public static class FoodUtility
    {
        public static bool GuestCanSatisfyFoodNeed(Pawn guest)
        {
            //Check Inventory
            var inventoryFood = RimWorld.FoodUtility.BestFoodInInventory(guest, minFoodPref: FoodPreferability.RawTasty);
            if (inventoryFood != null) return true;

            //Search FoodSource
            if (RimWorld.FoodUtility.TryFindBestFoodSourceFor(guest, guest, false, out var foodSource, out var foodDef, true, false, false, false, false, false, false, false, false, FoodPreferability.RawTasty))
            {
                if (foodSource != null && foodDef != null) return true;
            }

            return false;
        }

        public static bool TryPayForFood(Pawn buyerGuest, Building_NutrientPasteDispenser dispenser)
        {
            var vendingMachine = dispenser.TryGetComp<CompVendingMachine>();
            if (vendingMachine.IsActive() && dispenser.CanDispenseNow)
            {
                if (vendingMachine.IsFree) return true;

                if (!vendingMachine.CanAffordFast(buyerGuest, out Thing silver)) return false;

                vendingMachine.ReceivePayment(buyerGuest.inventory.innerContainer, silver);
                return true;
            }
            return false;
        }

        public static bool WillConsume(Pawn pawn, ThingDef foodDef)
        {
            if (foodDef == null) return false;
            var restrictions = pawn.foodRestriction.CurrentFoodRestriction;
            if (!restrictions.Allows(foodDef)) return false;

            var fineAsFood = foodDef.ingestible?.preferability == FoodPreferability.Undefined || foodDef.ingestible?.preferability == FoodPreferability.NeverForNutrition || pawn.WillEat(foodDef);
            return !foodDef.IsDrug && fineAsFood;
        }
    }
}
