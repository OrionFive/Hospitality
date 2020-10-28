using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace Hospitality.Harmony
{
    internal static class ForbidUtility_Patch
    {
        /// <summary>
        /// So guests will care
        /// </summary>
        [HarmonyPatch(typeof(ForbidUtility), nameof(ForbidUtility.CaresAboutForbidden))]
        public class CaresAboutForbidden
        {
            private static MethodInfo get_Map = AccessTools.Method(typeof(Thing), "get_Map");
            private static MethodInfo get_IsPlayerHome = AccessTools.Method(typeof(Map), "get_IsPlayerHome");

            /*
            IL_001D: ldarg.0
            IL_001E: callvirt  instance class Verse.Map Verse.Thing::get_Map()
            IL_0023: callvirt  instance bool Verse.Map::get_IsPlayerHome()
            IL_0028: brtrue.s  IL_0059 
            */
            
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source)
            {
                var list = source.ToList();
                int idx = 0;

                for (int i = 0; i < list.Count - 4; i++)
                {
                    var inst = list[i];

                    if (inst.opcode != OpCodes.Ldarg_0) continue;

                    // Need to make sure that our next two instructions are calls
                    var firstMethod = list[i + 1];
                    var secondMethod = list[i + 2];

                    if (firstMethod.opcode != OpCodes.Callvirt || secondMethod.opcode != OpCodes.Callvirt) continue;

                    // Make sure the following opcode is the branch we expect
                    var branch = list[i + 3];

                    if (branch.opcode != OpCodes.Brtrue_S) continue;

                    // Make sure our methods are calling the right things

                    if (firstMethod.operand as MethodInfo != get_Map) continue;
                    if (secondMethod.operand as MethodInfo != get_IsPlayerHome) continue;

                    idx = i;
                    break;
                }

                list.RemoveRange(idx, 4);

                return list;
            }
        }

        /// <summary>
        /// Set by JobDriver_Patch and stores who is doing a toil right now, in which case we don't want to forbid things.
        /// </summary>
        public static Pawn currentToilWorker;

        /// <summary>
        /// Things dropped by guests are never forbidden
        /// </summary>
        [HarmonyPatch(typeof(ForbidUtility), "SetForbidden")]
        public class SetForbidden
        {
            [HarmonyPrefix]
            public static bool Prefix(bool value)
            {
                if (value && currentToilWorker.IsArrivedGuest())
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Area check for guests trying to access things outside their zone.
        /// </summary>
        [HarmonyPatch(typeof(ForbidUtility), "InAllowedArea")]
        public class InAllowedArea
        {
            [HarmonyPostfix]
            public static void Postfix(IntVec3 c, Pawn forPawn, ref bool __result)
            {
                if (!__result) return; // Not ok anyway, moving on
                if (!forPawn.IsArrivedGuest()) return;

                var area = forPawn.GetGuestArea();
                if (area == null) return;
                if (!c.IsValid || !area[c]) __result = false;
            }
        }
    }
}
