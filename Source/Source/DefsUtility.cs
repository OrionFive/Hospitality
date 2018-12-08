using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Hospitality
{
    internal static class DefsUtility
    {
        /// <summary>
        /// Make sure other mods don't have invalid defs that I get the blame for when groups don't spawn...
        /// </summary>
        public static void CheckForInvalidDefs()
        {
            CheckChemicalDefs();
        }

        private static void CheckChemicalDefs()
        {
            var invalidDefs = DefDatabase<ChemicalDef>.AllDefsListForReading.Where(x => x.addictionHediff == null);

            foreach (var def in invalidDefs)
            {
                Log.ErrorOnce($"The ChemicalDef {def.defName} has no addictionHediff. Remove the ChemicalDef or add an addiction hediff. " 
                              + $"Otherwise this will cause random groups and raids to not spawn.", def.shortHash + 83747646);
            }
        }
    }
}