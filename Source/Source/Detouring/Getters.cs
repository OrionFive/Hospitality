using System.Reflection;
using RimWorld;
using Verse;

namespace Hospitality.Detouring
{
    public class ITab_Pawn_Guest : RimWorld.ITab_Pawn_Guest
    {
        // Added so guests will not show vanilla guest tab
        [Detour(typeof(RimWorld.ITab_Pawn_Guest), bindingFlags = BindingFlags.Public| BindingFlags.Instance)]
        public bool get_IsVisible()
        {
            if (SelPawn.HostFaction == Faction.OfPlayer) return !SelPawn.IsPrisoner && !SelPawn.IsGuest();
            return false;
        }
    }

    public class Pawn_PlayerSettings : RimWorld.Pawn_PlayerSettings
    {
        private Pawn pawn
        {
            get { return (Pawn)typeof(RimWorld.Pawn_PlayerSettings).GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this); }
        }

        // Added so guests will respect their assigned area
        [Detour(typeof(RimWorld.Pawn_PlayerSettings), bindingFlags = BindingFlags.Public | BindingFlags.Instance)]
        public bool get_RespectsAllowedArea()
        {
            return pawn.Faction == Faction.OfPlayer && pawn.HostFaction == null || pawn.IsGuest();
        }

        public Pawn_PlayerSettings(Pawn pawn) : base(pawn) {}
    }
}
