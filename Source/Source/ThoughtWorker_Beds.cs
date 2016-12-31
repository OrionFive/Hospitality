using System;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Hospitality
{
    /// <summary>
    /// Loaded via xml. Added so guests want beds.
    /// </summary>
    public class ThoughtWorker_Beds : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            try
            {
                if (pawn == null) return ThoughtState.Inactive;
                if (pawn.thingIDNumber == 0) return ThoughtState.Inactive; // What do you know!!!

                if (Current.ProgramState != ProgramState.Playing)
                {
                    return ThoughtState.Inactive;
                }
                if (!pawn.IsGuest()) return ThoughtState.Inactive;
                if(pawn.InBed()) return ThoughtState.Inactive;

                var room = pawn.GetGuestRoom();
                if (room == null) return ThoughtState.ActiveAtStage(0);
                if(room.Role==RoomRoleDefOf.None) return ThoughtState.Inactive;

                var groupSize = pawn.GetLord().ownedPawns.Count;
                var bedCount = room.ContainedBeds.Count(bed => bed is Building_GuestBed);

                if (bedCount == 0) return ThoughtState.ActiveAtStage(0);
                if (bedCount < groupSize) return ThoughtState.ActiveAtStage(1);
                if(bedCount > groupSize*1.3f && bedCount > groupSize+3) return ThoughtState.ActiveAtStage(3);
                return ThoughtState.ActiveAtStage(2);
            }
            catch(Exception e)
            {
                Log.Warning(e.Message);
                return ThoughtState.Inactive;
            }
        }
    }
}