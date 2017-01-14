using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace Hospitality.Detouring
{
    /// <summary>
    /// Added that guests can have this thought
    /// </summary>
    public static class ThoughtWorker_BedroomImpressiveness
    {
        [Detour(typeof (RimWorld.ThoughtWorker_BedroomImpressiveness), bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance)]
        public static ThoughtState CurrentStateInternal(RimWorld.ThoughtWorker_BedroomImpressiveness _this, Pawn p)
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

                if (room == null || room.Role != RoomRoleDefOf.Bedroom) return ThoughtState.Inactive;
                return result;
            }
#endregion

            // BASE
            if (p.ownership.OwnedBed.GetRoom().Role == RoomRoleDefOf.Bedroom)
            {
                return result;
            }
            return ThoughtState.Inactive;
        }
    }
}