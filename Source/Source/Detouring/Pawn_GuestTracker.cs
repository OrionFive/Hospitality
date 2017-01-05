using System.Reflection;
using Source = RimWorld.Pawn_GuestTracker;

namespace Hospitality.Detouring
{
    internal static class Pawn_GuestTracker
    {
        [Detour(typeof(Source), bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance)]
        internal static void Notify_PawnUndowned(Source _this)
        {
            // Just do nothing. We do the check somewhere else. Here is bad, because if the player rejects, the pawn will hang around way too long.
        }
    }
}