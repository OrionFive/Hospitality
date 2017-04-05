using System.Linq;
using System.Reflection;
using HugsLib.Source.Detour;
using RimWorld;
using Verse;

namespace Hospitality.Detouring
{
    /// <summary>
    /// Added that guests can have this thought
    /// </summary>
    public static class ThoughtWorker_BarracksImpressiveness
    {
        [DetourMethod(typeof(RimWorld.ThoughtWorker_BarracksImpressiveness), "CurrentStateInternal")]
        public static ThoughtState CurrentStateInternal(this RimWorld.ThoughtWorker_BarracksImpressiveness _this, Pawn p)
        {
            if (p == null) return ThoughtState.Inactive; // Added

            ThoughtState result = ThoughtWorker_SleepingRoomImpressiveness.CurrentStateInternal(_this, p); // Had to change

            if (!result.Active) return ThoughtState.Inactive; // Changed

#region Added
            if (p.IsGuest())
            {
                var beds = p.GetGuestBeds();
                if (!beds.Any()) return ThoughtState.Inactive;

                var room = beds.MinBy(b => b.Position.DistanceToSquared(p.PositionHeld)).GetRoom();
                
                if (room == null || room.Role != RoomRoleDefOf.Barracks) return ThoughtState.Inactive;
                return result;
            }
#endregion

            // BASE
            if (p.ownership.OwnedBed.GetRoom().Role == RoomRoleDefOf.Barracks)
            {
                return result;
            }
            return ThoughtState.Inactive;
        }
    }
}