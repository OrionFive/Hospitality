using System.Linq;
using System.Reflection;
using Verse;

namespace Hospitality.NoCCL
{
    public class SpecialInjector {
 
#if NoCCL
        public virtual bool Inject()
        {
            Log.Error("This should never be called.");
            return false;
        }
    }

    [StaticConstructorOnStartup]
    internal static class DetourInjector
    {
        private static Assembly Assembly { get { return Assembly.GetAssembly(typeof(DetourInjector)); } }
        private static string AssemblyName { get { return Assembly.FullName.Split(',').First(); } }
        static DetourInjector()
        {
            LongEventHandler.QueueLongEvent(Inject, "Initializing", true, null);
        }

        private static void Inject()
        {
            var injector = new Hospitality_SpecialInjector();
            if (injector.Inject()) Log.Message(AssemblyName + " injected.");
            else Log.Error(AssemblyName + " failed to get injected properly.");
        }
#endif
    }
}