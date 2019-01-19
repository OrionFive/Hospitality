using Harmony;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
    internal static class Pawn_GuestTracker_Patch
    {
        // I removed this feature
        //[HarmonyPatch(typeof(Pawn_GuestTracker), "Notify_PawnUndowned")]
        //public class Notify_PawnUndowned
        //{
        //    [HarmonyPrefix]
        //    internal static bool Replacement()
        //    {
        //        // Just do nothing. We do the check somewhere else. Here is bad, because if the player rejects, the pawn will hang around way too long.
        //        return false;
        //    }
        //}

        // Detoured so guests don't become prisoners
        [HarmonyPatch(typeof(Pawn_GuestTracker), "SetGuestStatus")]
        public class SetGuestStatus
        {
            [HarmonyPrefix]
            public static void Prefix(Pawn_GuestTracker __instance, ref bool prisoner)
            {
                // Added
                var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();

                if (pawn?.IsGuest() == true) prisoner = false;
            }
        }
    }
}