using RimWorld;
using Verse;

namespace Hospitality.Utilities
{
    public static class FoodUtility
    {
        public static bool GuestCanSatisfyFoodNeed(Pawn guest)
        {
            //Check Inventory
            //var inventoryFood = RimWorld.FoodUtility.BestFoodInInventory(guest, minFoodPref: FoodPreferability.RawTasty);
            //if (inventoryFood != null) return true;

            //Search FoodSource
            if (RimWorld.FoodUtility.TryFindBestFoodSourceFor(guest, guest, false, out var foodSource, out var foodDef, false, true, false, false, false, false, false, false, false, FoodPreferability.RawTasty))
            {
                if (foodSource != null && foodDef != null) return true;
            }
            return false;
        }

        public static bool GuestCanUseFoodSource(Pawn guest, Thing foodSource, ThingDef foodDef, bool desperate)
        {
            //If they own the food, they can simply eat it
            if (guest.inventory.Contains(foodSource))
            {
                return true;
            }

            //If they are starving, they simply take the next best food source
            if (desperate || guest.GetMapComponent().guestsCanTakeFoodForFree)
            {
                return true;
            }

            //If the food source is a gastronomy dining spot, allow to get food as well
            if (foodSource?.def == DefOf.Gastronomy_DiningSpot && foodDef != null)
            {
                Log.Message($"{guest.NameShortColored}: {foodSource?.LabelCap} ({foodSource?.Position}) is dining spot");
                return true;
            }

            //Check whether the current food source is a dispenser set as a vending machine for this guest
            Log.Message($"{guest.NameShortColored}: {foodSource.LabelCap} ({foodSource.Position}) is {foodSource.Label} (with food {foodDef?.label}). Is vending machine = {foodSource.TryGetComp<CompVendingMachine>() != null} CanUse = {foodSource.TryGetComp<CompVendingMachine>()?.CanBeUsedBy(guest, foodDef)??false}");
            if (foodSource is Building_NutrientPasteDispenser dispenser && (dispenser.TryGetComp<CompVendingMachine>()?.CanBeUsedBy(guest, foodDef) ?? false))
            {
                return true;
            }
            Log.Message($"{guest.NameShortColored}: {foodSource?.LabelCap} ({foodSource?.Position}) can't be used.");
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
