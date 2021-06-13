using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Hospitality
{
    public abstract class Alert_GuestThought : Alert_Guest
    {
        private static readonly List<Thought> tmpThoughts = new List<Thought>();
        protected abstract ThoughtDef Thought { get; }

        protected override List<Pawn> FindAffectedPawns()
        {
            affectedPawnsResult.Clear();
            foreach (var map in Find.Maps)
            foreach (var pawn in map.GetMapComponent().PresentGuests)
            {
                if (pawn.Dead) continue;

                if (pawn.needs.mood != null)
                {
                    pawn.needs.mood.thoughts.GetAllMoodThoughts(tmpThoughts);
                    try
                    {
                        foreach (var thought in tmpThoughts)
                        {
                            if (thought.def == Thought && !ThoughtUtility.ThoughtNullified(pawn, thought.def))
                                affectedPawnsResult.Add(pawn);
                        }
                    }
                    finally
                    {
                        tmpThoughts.Clear();
                    }
                }
            }

            return affectedPawnsResult;
        }
    }
}
