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
			if (need == null)
			{
				return 0f;
			}

			if ((int) pawn.needs.food.CurCategory < 3 && FoodUtility.ShouldBeFedBySomeone(pawn))
			{
				return 0f;
			}

			return GuestUtility.GetRequiresFoodFactor(pawn) * 7;
		}

		public override Job TryGiveJob(Pawn pawn)
		{
			if (pawn.needs.food == null) return null;
			joyDefBuyFood ??= DefDatabase<JoyGiverDef>.GetNamed("BuyFood");

			if (joyDefBuyFood.Worker.MissingRequiredCapacity(pawn) != null) return null;
			Log.Message($"{pawn.NameShortColored} is trying to buy food.");
			return joyDefBuyFood.Worker.TryGiveJob(pawn);
		}
	}
}
