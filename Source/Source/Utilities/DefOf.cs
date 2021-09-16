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

        public static JobDef VendingMachine_EmptyVendingMachine;
        public static RoomRoleDef GuestRoom;
        public static JobDef ClaimGuestBed;
    }
}
