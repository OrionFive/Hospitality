using Harmony;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
    internal static class Pawn_GuestTracker_Patch
    {
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