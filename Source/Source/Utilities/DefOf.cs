using RimWorld;
using Verse;

// ReSharper disable UnassignedField.Global

namespace Hospitality
{
    [RimWorld.DefOf]
    public static class DefOf
    {
        [MayRequire("CETeam.CombatExtended")]
        public static ThingDef Apparel_Backpack;

        [MayRequire("Orion.Gastronomy")]
        public static ThingDef Gastronomy_DiningSpot;

        public static JobDef VendingMachine_EmptyVendingMachine;
        public static RoomRoleDef GuestRoom;
        public static JobDef ClaimGuestBed;
        public static SpecialThingFilterDef AllowRotten;
    }
}
