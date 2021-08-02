using HarmonyLib;
using RimWorld;
using Verse;

namespace Hospitality.Patches
{
    /// <summary>
    /// So guests don't become prisoners
    /// </summary>
    internal static class Pawn_GuestTracker_Patch
    {
        [HarmonyPatch(typeof(Pawn_GuestTracker), nameof(Pawn_GuestTracker.SetGuestStatus))]
        public class SetGuestStatus
        {
            [HarmonyPrefix]
            public static void Prefix(ref GuestStatus guestStatus, Pawn ___pawn)
            {
                // Added
                if (___pawn?.IsGuest() == true) guestStatus = GuestStatus.Guest;
            }
        }
    }
}