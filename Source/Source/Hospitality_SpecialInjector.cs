using System;
using System.Linq;
using Verse;

namespace Hospitality
{

    public class Hospitality_SpecialInjector : SpecialInjector
    {
        public override bool Inject()
        {
            InjectTab(typeof(ITab_Pawn_Guest), def => def.race != null && def.race.Humanlike);

            InjectComp(typeof(CompProperties_Guest), def => def.race != null && def.race.Humanlike);

            return true;
        }

        private void InjectComp(Type compType, Func<ThingDef, bool> qualifier)
        {
            var defs = DefDatabase<ThingDef>.AllDefs.Where(qualifier).ToList();
            defs.RemoveDuplicates();

            foreach (var def in defs)
            {
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

            foreach (var def in defs)
            {
                if (!def.inspectorTabs.Contains(tabType))
                {
                    def.inspectorTabs.Add(tabType);
                    def.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(tabType));
                    //Log.Message(def.defName+": "+def.inspectorTabsResolved.Select(d=>d.GetType().Name).Aggregate((a,b)=>a+", "+b));
                }
            }
        }

    }
}
