using RimWorld;
using Verse;

namespace Hospitality
{
	/// <summary>
	/// Loaded via xml. Added so guests are upset when they have nothing to eat.
	/// </summary>
	public class ThoughtWorker_HasNoFood : ThoughtWorker
	{
		public override ThoughtState CurrentStateInternal(Pawn pawn)
		{
			if (pawn == null) return ThoughtState.Inactive;
			if (pawn.thingIDNumber == 0) return ThoughtState.Inactive; // What do you know!!!

			if (Current.ProgramState != ProgramState.Playing)
			{
				return ThoughtState.Inactive;
			}
			if (!pawn.IsArrivedGuest(out var compGuest)) return ThoughtState.Inactive;

			if (compGuest == null) return ThoughtState.Inactive;
			if(!compGuest.arrived) return ThoughtState.Inactive;

			var food = FoodUtility.BestFoodInInventory(pawn, minFoodPref: FoodPreferability.RawTasty);
			if(food != null) return ThoughtState.Inactive;

			return true;
		}
	}
}
