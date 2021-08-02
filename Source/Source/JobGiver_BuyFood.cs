using RimWorld;
using Verse;
using Verse.AI;

namespace Hospitality
{
	public class JobGiver_BuyFood : ThinkNode_JobGiver
	{
		private static JoyGiverDef joyDefBuyFood;
      
		public override float GetPriority(Pawn pawn)
		{
			if (!pawn.IsArrivedGuest(out _)) return 0;

			var need = pawn.needs.food;
			if (need == null) return 0;

			if ((int) pawn.needs.food.CurCategory < 3 && FoodUtility.ShouldBeFedBySomeone(pawn)) return 0;
			joyDefBuyFood ??= DefDatabase<JoyGiverDef>.GetNamed("BuyFood");

			var workerChance = joyDefBuyFood.Worker.GetChance(pawn) / joyDefBuyFood.Worker.def.baseChance;

			var requiresFoodFactor = GuestUtility.GetRequiresFoodFactor(pawn);
			if (requiresFoodFactor > 0.35f)
			{
				return requiresFoodFactor * 6;
			}
			var priority = requiresFoodFactor * workerChance;
			//Log.Message($"{pawn.NameShortColored} buy food priority: {priority:F2}; factor = {requiresFoodFactor}, worker chance = {workerChance}");
			return priority;
		}

		public override Job TryGiveJob(Pawn pawn)
		{
			if (pawn.needs.food == null) return null;

			if (joyDefBuyFood.Worker.MissingRequiredCapacity(pawn) != null) return null;
			//Log.Message($"{pawn.NameShortColored} is trying to buy food.");

			return joyDefBuyFood.Worker.TryGiveJob(pawn);
		}
	}
}
