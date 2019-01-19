using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using RimWorld;
using Verse;

namespace Hospitality
{

    public sealed class Hospitality_SpecialInjector
    {
        public void Inject()
        {
            InjectTab(typeof(ITab_Pawn_Guest), def => def.race?.Humanlike == true);

            InjectComp(typeof(CompProperties_Guest), def => def.race?.Humanlike == true);

            Type bed = typeof(Building_Bed);
            var bedDefs = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => bed.IsAssignableFrom(def.thingClass) && def.building.bed_humanlike).ToArray();

            var facilities = GetFacilitiesFor(bedDefs).ToArray();
            CreateGuestBedDefs(bedDefs, facilities);
        }

        private IEnumerable<CompProperties_Facility> GetFacilitiesFor(ThingDef[] bedDefs)
        {
            var sb = new StringBuilder("Injecting guest beds into the following facilities: ");
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                var comp = def.GetCompProperties<CompProperties_Facility>();
                if (comp == null) continue;
             
                if(comp.linkableBuildings == null) Log.Message("linkableBuildings == null");
                else if (comp.linkableBuildings.Any(bedDefs.Contains))
                {
                    sb.Append(def.defName + ", ");
                    yield return comp;
                }
            }
            Log.Message(sb.ToString().TrimEnd(' ', ','));
        }

        private void CreateGuestBedDefs(ThingDef[] bedDefs, CompProperties_Facility[] facilities)
        {
            var sb = new StringBuilder("Created guest beds for the following beds: ");
            var fields = typeof(ThingDef).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var bedDef in bedDefs)
            {
                var guestBedDef = new ThingDef();
                
                // Copy fields
                foreach (var field in fields)
                {
                    field.SetValue(guestBedDef, field.GetValue(bedDef));
                }

                CopyComps(guestBedDef, bedDef);

                // Other properties
                guestBedDef.defName += "Guest";
                guestBedDef.label = "GuestBedFormat".Translate(guestBedDef.label);
                guestBedDef.thingClass = typeof(Building_GuestBed);
                guestBedDef.shortHash = 0;
                guestBedDef.minifiedDef = null;
                guestBedDef.tradeability = Tradeability.None;
                guestBedDef.scatterableOnMapGen = false;

                typeof(ShortHashGiver).GetMethod("GiveShortHash", BindingFlags.NonPublic|BindingFlags.Static).Invoke(null, new object[] {guestBedDef, typeof(ThingDef)});
                DefDatabase<ThingDef>.Add(guestBedDef);
                sb.Append(bedDef.defName + ", ");

                foreach (var facility in facilities)
                {
                    facility.linkableBuildings.Add(guestBedDef);
                }
            }
            Log.Message(sb.ToString().TrimEnd(' ', ','));
        }

        private static void CopyComps(ThingDef guestBedDef, ThingDef bedDef)
        {
            guestBedDef.comps = new List<CompProperties>();
            for (int i = 0; i < bedDef.comps.Count; i++)
            {
                var constructor = bedDef.comps[i].GetType().GetConstructor(Type.EmptyTypes);
                var comp = (CompProperties) constructor.Invoke(null);

                var fieldsComp = comp.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fieldsComp)
                {
                    field.SetValue(comp, field.GetValue(bedDef.comps[i]));
                }
                guestBedDef.comps.Add(comp);
            }
        }

        private void InjectComp(Type compType, Func<ThingDef, bool> qualifier)
        {
            var defs = DefDatabase<ThingDef>.AllDefsListForReading.Where(qualifier).ToList();
            defs.RemoveDuplicates();

            foreach (var def in defs)
            {
                if (def.comps == null) continue;

                if (!def.comps.Any(c=>c.GetType() == compType))
                {
                    def.comps.Add((CompProperties) Activator.CreateInstance(compType));
                    //Log.Message(def.defName+": "+def.inspectorTabsResolved.Select(d=>d.GetType().Name).Aggregate((a,b)=>a+", "+b));
                }
            }
        }

        private void InjectTab(Type tabType, Func<ThingDef, bool> qualifier)
        {
            var defs = DefDatabase<ThingDef>.AllDefs.Where(qualifier).ToList();
            defs.RemoveDuplicates();

            var tabBase = InspectTabManager.GetSharedInstance(tabType);

            foreach (var def in defs)
            {
                if (def.inspectorTabs == null || def.inspectorTabsResolved == null) continue;

                if (!def.inspectorTabs.Contains(tabType))
                {
                    def.inspectorTabs.Add(tabType);
                    def.inspectorTabsResolved.Add(tabBase);
                    //Log.Message(def.defName+": "+def.inspectorTabsResolved.Select(d=>d.GetType().Name).Aggregate((a,b)=>a+", "+b));
                }
            }
        }

    }
}
