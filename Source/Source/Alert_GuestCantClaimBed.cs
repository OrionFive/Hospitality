using RimWorld;
using Verse;

namespace Hospitality
{
    public class Alert_GuestCantClaimBed : Alert_GuestThought
    {
        public Alert_GuestCantClaimBed()
        {
            defaultLabel = "AlertCantClaimBed".Translate();
            explanationKey = "AlertCantClaimBedDesc";
            Log.Message($"Created CantClaimBed alert for {Thought.Label}.");
        }

        protected override ThoughtDef Thought => DefDatabase<ThoughtDef>.GetNamed("GuestCantAffordBed");

        public override void AlertActiveUpdate()
        {
            Log.Message($"Updating alert... {AffectedPawns.Count} pawns affected.");
            base.AlertActiveUpdate();
        }
    }
}
