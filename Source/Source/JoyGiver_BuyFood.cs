using System.Linq;
using RimWorld;
using Verse;

namespace Hospitality
{
    public class JoyGiver_BuyFood : JoyGiver_BuyStuff
    {
        protected override ThingRequestGroup RequestGroup { get { return ThingRequestGroup.FoodSourceNotPlantOrTree; } }

        public override float GetChance(Pawn pawn)
        {
            if (!pawn.IsGuest()) return 0;

            var carriedFood = pawn.inventory.innerContainer.Count(i => i.IngestibleNow);
            var needFood = pawn.needs.TryGetNeed<Need_Food>();
            var hungerFactor = needFood != null ? needFood.PercentageThreshHungry : 0;
            var carriedFactor = carriedFood == 0 ? 1 : carriedFood == 1 ? 0.25f : 0.05f;

            //Log.Message(pawn.NameStringShort+" - wanna buy food: "+hungerFactor*carriedFactor);
            return base.GetChance(pawn)*hungerFactor*carriedFactor;
        }

        protected override bool Qualifies(Thing thing)
        {
            return thing.IngestibleNow;
        }
    }
}