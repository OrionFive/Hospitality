using Verse;

namespace Hospitality.Detouring
{
    /// <summary>
    /// Allow only visitors to use only guest beds
    /// </summary>
    internal static class RestUtility {
        [Detour(typeof(RimWorld.RestUtility))]
        public static bool CanUseBedEver(Pawn p, ThingDef bedDef)
        {
#region Added
            if (bedDef.thingClass == typeof (Building_GuestBed))
            {
                return p.IsGuest();
            }
#endregion
            return p.BodySize <= bedDef.building.bed_maxBodySize && p.RaceProps.Humanlike == bedDef.building.bed_humanlike; // BASE
        }
    }
}