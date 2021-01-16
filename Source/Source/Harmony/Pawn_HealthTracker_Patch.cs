using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
	internal static class Pawn_HealthTracker_Patch
	{
		/// <summary>
		/// Friendly pawns shouldn't litter all their stuff when they get downed.
		/// </summary>
		[HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
		public class MakeDowned
		{
			private static MethodInfo dropAndForbidMethod = AccessTools.Method(typeof(Pawn),  nameof(Pawn.DropAndForbidEverything));
			private static MethodInfo dropAndForbidReplacement = AccessTools.Method(typeof(Pawn_HealthTracker_Patch),  nameof(DropAndForbidEverythingReplacement));

			[HarmonyTranspiler]    
			public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source)
			{
				//Log.Message($"Hospitality patching: {dropAndForbidMethod?.FullDescription()} >> {dropAndForbidReplacement?.FullDescription()}");
				foreach (var instruction in source)
				{
					//IL_00d3: callvirt instance void Verse.Pawn::DropAndForbidEverything(bool)
					if (instruction.opcode == OpCodes.Callvirt && instruction.operand.Equals(dropAndForbidMethod))
					{
					    var replacement = instruction.Clone(dropAndForbidReplacement);
					    //Log.Message($"Replaced instruction {instruction.operand} with {replacement.operand}.");
					    yield return replacement;
					}
					else yield return instruction;
				}
			}
		}

		[UsedImplicitly]
		public static void DropAndForbidEverythingReplacement(this Pawn pawn, bool keepInventoryAndEquipmentIfInBed)
		{
			if (!Settings.disableFriendlyGearDrops || pawn?.Faction == null || pawn.HostileTo(Faction.OfPlayer))
			{
				// Run original code
				pawn.DropAndForbidEverything(keepInventoryAndEquipmentIfInBed);
				return;
			}

			// Is not hostile, do nothing
		}
	}
}