using System.Linq;
using Verse;
using Verse.AI.Group;

namespace Hospitality
{
    public class VisitorFlag : ThingWithComps
    {
        private Lord lord;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref lord, "lord");
        }

        public void SetLord(Lord lord)
        {
            this.lord = lord;
        }

        public override string GetInspectString()
        {
            if (lord == null) return base.GetInspectString();

            var names = GenText.ToCommaList(lord.ownedPawns.Select(p => p.NameStringShort));
            return string.Format("Visitors from {0}:\n{1}", lord.faction, names);
        }
    }
}