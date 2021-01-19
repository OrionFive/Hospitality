using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
	internal static class NoLittering_Patch
	{
		/// <summary>
		/// Friendly pawns shouldn't litter all their stuff when they get downed.
		/// </summary>
		[HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
		public class MakeDowned
		{
			private static readonly MethodInfo original = AccessTools.Method(typeof(Pawn), nameof(Pawn.DropAndForbidEverything));
			private static readonly MethodInfo replacement = AccessTools.Method(typeof(NoLittering_Patch), nameof(DropAndForbidEverythingReplacement));

			[HarmonyTranspiler]    
			public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source)
			{
				//Log.Message($"Hospitality patching: {original?.FullDescription()} >> {replacement?.FullDescription()}");
				foreach (var instruction in source)
				{
					//IL_00d3: callvirt instance void Verse.Pawn::DropAndForbidEverything(bool)
					if (instruction.opcode == OpCodes.Callvirt && instruction.operand.Equals(original))
					{
						//Log.Message($"Replaced instruction {instruction.operand} with {replacement}.");
						var replaced = instruction.Clone(replacement);
						yield return replaced;
					}
					else yield return instruction;
				}
			}
		}

		[UsedImplicitly]
		public static void DropAndForbidEverythingReplacement(this Pawn pawn, bool keepInventoryAndEquipmentIfInBed)
		{
			if (ShouldDrop(pawn))
			{
				// Run original code
				pawn.DropAndForbidEverything(keepInventoryAndEquipmentIfInBed);
				return;
			}

			// Is not hostile, do nothing
		}

		private static bool ShouldDrop(Pawn pawn)
		{
			return !Settings.disableFriendlyGearDrops || pawn?.Faction == null || pawn.HostileTo(Faction.OfPlayer) || pawn.Faction == Faction.OfPlayer;
		}

		[HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.Notify_PawnSpawned))]
		public class Notify_PawnSpawned
		{
			private static readonly MethodInfo original = AccessTools.Method(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.DropAllEquipment));
			private static readonly MethodInfo replacement = AccessTools.Method(typeof(NoLittering_Patch), nameof(DropAllEquipmentReplacement));

			[HarmonyTranspiler]    
			public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source)
			{
				foreach (var instruction in source)
				{
					//IL_004a: call instance void Verse.Pawn_EquipmentTracker::DropAllEquipment(valuetype Verse.IntVec3, bool)
					if (instruction.opcode == OpCodes.Call && instruction.operand.Equals(original))
					{
						//Log.Message($"Replaced instruction {instruction.operand} with {replacement}.");
						var replaced = instruction.Clone(replacement);
						yield return replaced;
					}
					else yield return instruction;
				}
			}
		}

		[UsedImplicitly]
		public static void DropAllEquipmentReplacement(this Pawn_EquipmentTracker equipment, IntVec3 pos, bool forbid = true)
		{
			if (ShouldDrop(equipment.pawn))
			{
				// Run original code
				equipment.DropAllEquipment(pos, forbid);
			}

			// Is not hostile, do nothing
		}
	}
}