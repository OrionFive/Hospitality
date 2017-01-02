using System.Reflection;
using RimWorld;
using Verse;

namespace Hospitality.Detouring
{
    /// <summary>
    /// Against rare error, when guest's ownership.OwnedBed == null 
    /// </summary>
    public class ThoughtWorker_PrisonBarracksImpressiveness
    {
        [Detour(typeof(RimWorld.ThoughtWorker_PrisonBarracksImpressiveness), bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance)]
        public static ThoughtState CurrentStateInternal(RimWorld.ThoughtWorker_PrisonBarracksImpressiveness _this, Pawn p)
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