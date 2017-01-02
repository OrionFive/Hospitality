using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;
using Source = RimWorld.JoyGiver_Ingest;

namespace Hospitality.Detouring
{
    internal static class JoyGiver_Ingest
    {
        [Detour(typeof(Source), bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance)]
        public static bool CanUseIngestItemForJoy(Pawn pawn, Thing t)
        {
            if (!t.def.IsIngestible) return false; // Added to prevent error (t.def is human... wtf RimWorld)

            if (t.def.ingestible.joyKind == null || t.def.ingestible.joy <= 0f)
            {
                return false;
            }

            if (t.Spawned)
            {
                if (!pawn.CanReserve(t, 1))
                {
                    return false;
                }
                if (t.IsForbidden(pawn))
                {
                    return false;
                }
                if (!t.IsSociallyProper(pawn))
                {
                    return false;
                }
            }
            //Log.Message(t.def.IsDrug + ": " + (pawn.drugs == null));
            if (t.def.IsDrug && pawn.drugs != null && !pawn.drugs.CurrentPolicy[t.def].allowedForJoy
                && pawn.story != null)
            {
                int num = pawn.story.traits.DegreeOfTrait(TraitDefOf.DrugDesire);
                if (num <= 0 && !pawn.InMentalState)
                {
                    return false;
                }
            }
            return true;
        }
    }
}