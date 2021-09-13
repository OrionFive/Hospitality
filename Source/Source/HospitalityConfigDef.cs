using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Hospitality
{
    public class HospitalityConfigDef : Def
    {
        public List<ThingDef> vendingMachines;

        public static HospitalityConfigDef Config => DefDatabase<HospitalityConfigDef>.GetNamed("MainConfig");
    }
}
