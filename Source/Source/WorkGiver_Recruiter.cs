using RimWorld;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public class WorkGiver_Recruiter : WorkGiver_Scanner
    {
        private readonly JobDef jobDefRecruit = DefDatabase<JobDef>.GetNamed("CharmGuest");

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var guest = t as Pawn;
            return pawn.ShouldRecruit(guest);
        }

        public override bool ShouldSkip(Pawn pawn, bool forced)
        {
            return pawn.story.WorkTagIsDisabled(def.workType.workTags);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return new Job(jobDefRecruit, t);
        }

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override PathEndMode PathEndMode => PathEndMode.OnCell;
    }
}
