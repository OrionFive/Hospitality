using RimWorld;
using Verse;

// ReSharper disable InconsistentNaming
// ReSharper disable UnassignedField.Global

namespace Hospitality.Utilities
{
    [DefOf]
    public static class InternalDefOf
    {
        [MayRequire("CETeam.CombatExtended")]
        public static ThingDef Apparel_Backpack;

        [MayRequire("VanillaExpanded.VMemesE")]
        public static PreceptDef VME_Anonymity_Required;

        [MayRequire("Orion.Gastronomy")]
        public static ThingDef Gastronomy_DiningSpot;

        public static JobDef VendingMachine_EmptyVendingMachine;
        public static RoomRoleDef GuestRoom;
        public static JobDef ClaimGuestBed;
        public static SpecialThingFilterDef AllowRotten;
        public static ThoughtDef GuestExpensiveFood;
        public static ThoughtDef GuestCheapFood;
        public static ConceptDef GuestWork;
        public static JoyGiverDef BuyFood;
    }
}
