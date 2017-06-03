using System.Linq;
using System.Reflection;
using HugsLib;
using Verse;

namespace Hospitality
{
    public class SpecialInjector
    {
        public virtual bool Inject()
        {
            Log.Error("This should never be called.");
            return false;
        }
    }

    internal class DetourInjector : ModBase
    {
        private static Assembly Assembly { get { return Assembly.GetAssembly(typeof(DetourInjector)); } }
        private static string AssemblyName { get { return Assembly.FullName.Split(',').First(); } }

        private static void Inject()
        {
            var injector = new Hospitality_SpecialInjector();
            if (injector.Inject()) Log.Message(AssemblyName + " injected.");
            else Log.Error(AssemblyName + " failed to get injected properly.");
        }

        public override string ModIdentifier { get { return "Hospitality"; } }

        public override void Initialize()
        {
            Inject();
        }
    }
}
