using System.Linq;
using RimWorld;
using Verse;

namespace Hospitality
{
    public class JoyGiver_BuyFood : JoyGiver_BuyStuff
    {
        protected override ThingRequestGroup RequestGroup => ThingRequestGroup.FoodSourceNotPlantOrTree;

        public override float GetChance(Pawn pawn)
        {
            if (!pawn.IsGuest()) return 0;

            var carriedFood = pawn.inventory.innerContainer.Count(CanEat);
            var needFood = pawn.needs.TryGetNeed<Need_Food>();
            var hungerFactor = 1 - needFood?.CurLevelPercentage ?? 1;
            var carriedFactor = carriedFood == 0 ? 1 : carriedFood == 1 ? 0.25f : 0.05f;

            //Log.Message(pawn.NameStringShort+" - wanna buy food: "+hungerFactor*carriedFactor);
            return base.GetChance(pawn)*hungerFactor*carriedFactor;
        }

        private static bool CanEat(Thing thing)
        {
            return thing.def.IsNutritionGivingIngestible && thing.def.IsWithinCategory(ThingCategoryDefOf.Foods);
        }

        protected override bool Qualifies(Thing thing, Pawn pawn)
        {
            return CanEat(thing);
        }
    }
}