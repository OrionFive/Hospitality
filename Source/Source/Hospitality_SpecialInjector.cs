using System;
using System.Reflection;
using System.Linq;
using Verse;

// Toggle in Hospitality Properties
#if NoCCL
using Hospitality.NoCCL;
#else
using CommunityCoreLibrary;
#endif

namespace Hospitality
{

    public class Hospitality_SpecialInjector : SpecialInjector
    {

        private static Assembly Assembly { get { return Assembly.GetAssembly(typeof(Hospitality_SpecialInjector)); } }

        private static readonly BindingFlags[] bindingFlagCombos = {
            BindingFlags.Instance | BindingFlags.Public, BindingFlags.Static | BindingFlags.Public,
            BindingFlags.Instance | BindingFlags.NonPublic, BindingFlags.Static | BindingFlags.NonPublic,
        };

        public override bool Inject()
        {
            // Special detours
            #region Special detours
            // Change guest bed gizmos to default building gizmos
            if(!Detours.TryDetourFromTo(
                typeof (Building_GuestBed).GetMethod("GetGizmos", BindingFlags.Instance | BindingFlags.Public),
                typeof (Building).GetMethod("GetGizmos", BindingFlags.Instance | BindingFlags.Public))) return false;
            #endregion

            #region Automatic hookup
            // Loop through all detour attributes and try to hook them up
            foreach (var targetType in Assembly.GetTypes())
            {
                foreach (var bindingFlags in bindingFlagCombos)
                {
                    foreach (var targetMethod in targetType.GetMethods(bindingFlags))
                    {
                        foreach (DetourAttribute detour in targetMethod.GetCustomAttributes(typeof(DetourAttribute), true))
                        {
                            var flags = detour.bindingFlags != default(BindingFlags) ? detour.bindingFlags : bindingFlags;
                            var sourceMethod = detour.source.GetMethod(targetMethod.Name, flags);
                            if (sourceMethod == null)
                            {
                                Log.Error( string.Format( "Hospitality :: Detours :: Can't find source method '{0} with bindingflags {1}", targetMethod.Name, flags ) );
                                return false;
                            }
                            if (!Detours.TryDetourFromTo(sourceMethod, targetMethod)) return false;
                        }
                    }
                }
            }
            #endregion

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
                    def.inspectorTabsResolved.Add(ITabManager.GetSharedInstance(tabType));
                    //Log.Message(def.defName+": "+def.inspectorTabsResolved.Select(d=>d.GetType().Name).Aggregate((a,b)=>a+", "+b));
                }
            }
        }

    }
}
