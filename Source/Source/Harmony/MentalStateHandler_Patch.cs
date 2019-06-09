using Harmony;
using Verse;
using Verse.AI;

namespace Hospitality.Harmony {
    /// <summary>
    /// So guests don't break while arriving or leaving
    /// </summary>
    public class MentalStateHandler_Patch
    {
        [HarmonyPatch(typeof(MentalStateHandler), "TryStartMentalState")]

        public class TryStartMentalState
        {
            [HarmonyPrefix]
            public static bool Prefix(MentalStateHandler __instance)
            {
                var pawn = Traverse.Create(__instance).Field<Pawn>("pawn").Value;
                if (pawn != null && pawn.IsGuest() && !pawn.IsArrived())
                {
                    Log.Message($"{pawn.LabelShort} was about to have a mental break. Cancelled, because guest is traveling.");
                    return false;
                }

                return true;
            }
        }
    }
}