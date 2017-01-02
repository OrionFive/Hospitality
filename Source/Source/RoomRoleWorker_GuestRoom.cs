using RimWorld;
using Verse;

namespace Hospitality
{
    public class RoomRoleWorker_GuestRoom : RoomRoleWorker
    {
        public override float GetScore(Room room)
        {
            int num = 0;
            var allContainedThings = room.AllContainedThings;
            foreach (var thing in allContainedThings)
            {
                var building_Bed = thing as Building_Bed;
                if (building_Bed != null && building_Bed.def.building.bed_humanlike)
                {
                    if (building_Bed.ForPrisoners) return 0;
                }
                var building_GuestBed = thing as Building_GuestBed;
                if (building_GuestBed != null && building_GuestBed.def.building.bed_humanlike)
                {
                    num++;
                }
            }
            if (num < 1) return 0;
            return num*110000;
        }
    }
}