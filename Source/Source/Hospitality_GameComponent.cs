using System.Linq;
using RimWorld;
using Verse;

namespace Hospitality
{
	public class Hospitality_GameComponent : GameComponent
	{
		public FoodRestriction defaultFoodRestriction;

		// ReSharper disable once UnusedParameter.Local
		public Hospitality_GameComponent(Game game)
		{
			// Bug: Why does this run 3 times when loading a game?
			defaultFoodRestriction = CreateDefaultFoodRestriction();
		}

		public override void ExposeData()
		{
			Scribe_Deep.Look(ref defaultFoodRestriction, "defaultFoodRestriction");
		}

		public static FoodRestriction CreateDefaultFoodRestriction()
		{
			const int id = 600; // Arbitrary number
			FoodRestriction foodRestriction = new FoodRestriction(id, "Hospitality_Guests");
			foodRestriction.filter.SetAllow(ThingCategoryDefOf.Foods, true);
			foodRestriction.filter.SetAllow(ThingCategoryDefOf.CorpsesHumanlike, false);
			foodRestriction.filter.SetAllow(ThingCategoryDefOf.CorpsesAnimal, false);
			foodRestriction.filter.SetAllow(ThingCategoryDefOf.MeatRaw, false);

			//Log.Message($"Guest food restriction: {foodRestriction.filter.allowedDefs.Where(d=>foodRestriction.Allows(d)).Select(d=>d.label).ToCommaList()}");

			return foodRestriction;
		}


	}
}
