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
            [HarmonyPostfix]
            public static void Postfix(MentalStateHandler __instance, ref bool __result)
            {
                if (!__result) return;

                var pawn = Traverse.Create(__instance).Field<Pawn>("pawn").Value;
                if (pawn.IsGuest() && !pawn.IsArrived())
                {
                    __result = false;
                    Log.Message($"{pawn.LabelShort} was about to have a mental break. Cancelled, because guest is traveling.");
                }
            }
        }
    }
}