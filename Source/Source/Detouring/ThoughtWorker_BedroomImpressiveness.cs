using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Hospitality.Detouring
{
    /// <summary>
    /// Added that guests can have this thought
    /// </summary>
    public static class ThoughtWorker_BedroomImpressiveness
    {
        private static readonly RoomRoleDef guestroomRoleDef = DefDatabase<RoomRoleDef>.GetNamed("GuestRoom");

        [Detour(typeof (RimWorld.ThoughtWorker_BedroomImpressiveness), bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance)]
        public static ThoughtState CurrentStateInternal(RimWorld.ThoughtWorker_BedroomImpressiveness _this, Pawn p)
        {
            if (p == null) return ThoughtState.Inactive; // Added

            ThoughtState result = ThoughtWorker_SleepingRoomImpressiveness.CurrentStateInternal(_this, p); // Had to change
            
            if (!result.Active) return ThoughtState.Inactive; // Changed

#region Added
            if (p.IsGuest())
            {
                var room = p.GetGuestRoom();
                if (room != null && room.Role == guestroomRoleDef)
                {
                    return result;
                }
                return ThoughtState.Inactive;
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