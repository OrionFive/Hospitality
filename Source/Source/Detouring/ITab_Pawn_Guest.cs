using RimWorld;
using Verse;
using Source = RimWorld.ITab_Pawn_Guest;

namespace Hospitality.Detouring
{
    public class ITab_Pawn_Guest : Source
    {
        // Detoured from SpecialInjector
        public bool IsVisible_Get()
        {
            if (SelPawn.HostFaction == Faction.OfPlayer) return !SelPawn.IsPrisoner && !SelPawn.IsGuest();
            return false;
        }
    }
}
