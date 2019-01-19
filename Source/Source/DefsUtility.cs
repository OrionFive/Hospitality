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
            CheckFactionDefs();
        }

        // Must have at least one pawn group maker of type "Peaceful", if ever non hostile
        private static void CheckFactionDefs()
        {
            foreach (var factionDef in DefDatabase<FactionDef>.AllDefsListForReading.Where(f=>!f.isPlayer && !f.hidden && f.CanEverBeNonHostile))
            {
                if (factionDef.pawnGroupMakers?.Any(pgm => pgm.kindDef.defName == "Peaceful") != true)
                {
                    LogMisconfiguration(factionDef, $"FactionDef {factionDef.defName} must have at least one pawnGroupMaker with kindDef 'Peaceful', or 'permanentEnemy', 'isPlayer' or 'hidden' must be set to true.");
                }
            }
        }

        private static void CheckChemicalDefs()
        {
            foreach (var def in DefDatabase<ChemicalDef>.AllDefsListForReading.Where(x => x.addictionHediff == null))
            {
                LogMisconfiguration(def, $"The ChemicalDef {def.defName} has no addictionHediff. Remove the ChemicalDef or add an addiction hediff. Otherwise this will cause random groups and raids to not spawn.");
            }
        }

        private static void LogMisconfiguration(Def def, string message)
        {
            //var commaList = LoadedModManager.RunningModsListForReading.Where(m => m.AllDefs.Contains(def)).Select(m => m.Name).ToCommaList(true);
            var modName = def.modContentPack == null ? "unknown mod" : def.modContentPack.Name;
            Log.ErrorOnce($"{message} This is a misconfiguration in {modName}.", def.shortHash + 83747646);
        }
    }
}