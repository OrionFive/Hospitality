using System.Collections.Generic;
using Harmony;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
    /// <summary>
    /// Different expectations for guests (and expectations at all!)
    /// </summary>
    public class ExpectationsUtility_Patch
    {
        [HarmonyPatch(typeof(ExpectationsUtility), "CurrentExpectationFor", typeof(Pawn))]
        public class CurrentExpectationForPawn
        {
            [HarmonyPostfix]
            public static void Postfix(ref ExpectationDef __result, Pawn p)
            {
                if (__result == null) return; // Original method aborted, so will we
                if (p.IsGuest())
                {
                    __result = CurrentExpectationFor(p.MapHeld);
                }
            }

            // Copied
            private static ExpectationDef CurrentExpectationFor(Map m)
            {
                float wealthTotal = m.wealthWatcher.WealthTotal * 2; // Doubled for guests
                var list = Traverse.Create(typeof(ExpectationsUtility)).Field("expectationsInOrder").GetValue<List<ExpectationDef>>(); // had to add
                foreach (ExpectationDef expectationDef in list) 
                {
                    if (wealthTotal < expectationDef.maxMapWealth)
                    {
                        return expectationDef;
                    }
                }
                return list[list.Count - 1];
            }
        }
    }
}