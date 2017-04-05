using System.Reflection;
using HugsLib.Source.Detour;
using RimWorld;
using Verse;

namespace Hospitality.Detouring
{
    /// <summary>
    /// Against rare error, when guest's ownership.OwnedBed == null 
    /// </summary>
    public static class ThoughtWorker_PrisonBarracksImpressiveness
    {
        [DetourMethod(typeof(RimWorld.ThoughtWorker_PrisonBarracksImpressiveness), "CurrentStateInternal")]
        public static ThoughtState CurrentStateInternal(this RimWorld.ThoughtWorker_PrisonBarracksImpressiveness _this, Pawn p)
        {
            if (p == null || p.ownership == null || p.ownership.OwnedBed == null) return ThoughtState.Inactive; // Added
            ThoughtState result = ThoughtWorker_SleepingRoomImpressiveness.CurrentStateInternal(_this, p); // Had to change

            // BASE
            if (result.Active && p.ownership.OwnedBed.GetRoom().Role == RoomRoleDefOf.PrisonBarracks)
            {
                return result;
            }
            return ThoughtState.Inactive;
        }
    }
}