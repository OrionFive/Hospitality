using System.Reflection;
using HugsLib.Source.Detour;
using RimWorld;
using Verse;

namespace Hospitality.Detouring
{
    /// <summary>
    /// Added that guests can NOT have this thought
    /// </summary>
    internal static class ThoughtWorker_SharedBedroom
    {
        [DetourMethod(typeof(RimWorld.ThoughtWorker_SharedBedroom), "CurrentStateInternal")]
        public static ThoughtState CurrentStateInternal(this RimWorld.ThoughtWorker_SharedBedroom _this, Pawn p)
        {
            return p != null && !p.IsGuest() &&p.ownership != null && p.ownership.OwnedBed != null && p.ownership.OwnedRoom == null
                   && !p.ownership.OwnedBed.GetRoom().PsychologicallyOutdoors;
        }
    }
}