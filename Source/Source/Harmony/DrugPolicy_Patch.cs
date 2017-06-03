using System;
using System.Collections.Generic;
using Harmony;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
    public class DrugPolicy_Patch
    {
        [HarmonyPatch(typeof(DrugPolicy), "get_Item", new[] {typeof(ThingDef)})]
        public class Item
        {
            [HarmonyPostfix]
            public static void Postfix(DrugPolicy __instance, ref DrugPolicyEntry __result, ThingDef drugDef)
            {
                var entriesInt = Traverse.Create(__instance).Field("entriesInt").GetValue<List<DrugPolicyEntry>>();

                foreach (DrugPolicyEntry entry in entriesInt)
                {
                    if (entry.drug == drugDef)
                    {
                        __result = entry;
                        return;
                    }
                }
                // Added: Missing def
                __result = AddDef(drugDef, entriesInt);
            }

            private static DrugPolicyEntry AddDef(ThingDef drugDef, List<DrugPolicyEntry> entriesInt)
            {
                if (drugDef.category == ThingCategory.Item && drugDef.IsDrug)
                {
                    Log.Message("Processed " + drugDef);
                    DrugPolicyEntry drugPolicyEntry = new DrugPolicyEntry { drug = drugDef, allowedForAddiction = true };
                    entriesInt.Add(drugPolicyEntry);
                    entriesInt.SortBy(e => e.drug.GetCompProperties<CompProperties_Drug>().listOrder);

                    return drugPolicyEntry;
                }
                throw new Exception("DrugDef " + drugDef.LabelCap + " is not a drug or of ThingCategory Item.");
            }
        }
    }
}