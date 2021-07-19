using RimWorld;
using Verse;

namespace Hospitality
{
	public class Alert_GuestHasNoFood : Alert_GuestThought
	{
		public Alert_GuestHasNoFood()
		{
			defaultLabel = "AlertHasNoFood".Translate();
			explanationKey = "AlertHasNoFoodDesc";
		}

		protected override ThoughtDef Thought => DefDatabase<ThoughtDef>.GetNamed("GuestHasNoFood");
		private protected override int Hash => 7424;
	}
}
