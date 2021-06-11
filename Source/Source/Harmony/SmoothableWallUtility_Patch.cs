using HarmonyLib;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
	public class SmoothableWallUtility_Patch
	{
		/// <summary>
		/// So guests smoothing walls don't claim them for their faction
		/// </summary>
		[HarmonyPatch(typeof(SmoothableWallUtility), nameof(SmoothableWallUtility.SmoothWall))]
		public class SmoothWall
		{
			[HarmonyPostfix]
			public static void Postfix(Thing target, Pawn smoother)
			{
				if (smoother.HostFaction != smoother.Faction)
				{
					target.SetFaction(smoother.HostFaction);
				}
			}
		}
	}
}
