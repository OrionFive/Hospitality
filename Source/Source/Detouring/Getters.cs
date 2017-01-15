using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace Hospitality.Detouring
{
    public class ITab_Pawn_Guest : RimWorld.ITab_Pawn_Guest
    {
        // Added so guests will not show vanilla guest tab
        [Detour(typeof(RimWorld.ITab_Pawn_Guest), bindingFlags = BindingFlags.Public| BindingFlags.Instance)]
        public bool get_IsVisible()
        {
            if (SelPawn.HostFaction == Faction.OfPlayer) return !SelPawn.IsPrisoner && !SelPawn.IsGuest();
            return false;
        }
    }

    public class Pawn_PlayerSettings : RimWorld.Pawn_PlayerSettings
    {
        private Pawn pawn
        {
            get { return (Pawn)typeof(RimWorld.Pawn_PlayerSettings).GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this); }
        }

        // Added so guests will respect their assigned area
        [Detour(typeof(RimWorld.Pawn_PlayerSettings), bindingFlags = BindingFlags.Public | BindingFlags.Instance)]
        public bool get_RespectsAllowedArea()
        {
            return pawn.Faction == Faction.OfPlayer && pawn.HostFaction == null || pawn.IsGuest();
        }

        public Pawn_PlayerSettings(Pawn pawn) : base(pawn) {}
    }

    // This is so unknown drug policies get added automatically
    public class DrugPolicy : RimWorld.DrugPolicy
    {
        //Detoured from SpecialInjector due to ambiguity
        public DrugPolicyEntry Item(ThingDef drugDef)
        {
            var entriesInt = (List<DrugPolicyEntry>) GetType().GetField("entriesInt", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);

            foreach (DrugPolicyEntry entry in entriesInt)
            {
                if (entry.drug == drugDef)
                {
                    return entry;
                }
            }
            // Added: Missing def
            return AddDef(drugDef, entriesInt);
        }

        private static DrugPolicyEntry AddDef(ThingDef drugDef, List<DrugPolicyEntry> entriesInt)
        {
            if (drugDef.category == ThingCategory.Item && drugDef.IsDrug)
            {
                Log.Message("Processed " + drugDef);
                DrugPolicyEntry drugPolicyEntry = new DrugPolicyEntry {drug = drugDef, allowedForAddiction = true};
                entriesInt.Add(drugPolicyEntry);
                entriesInt.SortBy(e => e.drug.GetCompProperties<CompProperties_Drug>().listOrder);

                return drugPolicyEntry;
            }
            throw new Exception("DrugDef " + drugDef.LabelCap + " is not a drug or of ThingCategory Item.");
        }
    }

}