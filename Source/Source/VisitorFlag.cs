using Verse;

namespace Hospitality
{
    // TODO: Remove in B19
    public class VisitorFlag : ThingWithComps
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            Destroy();
        }
    }
}