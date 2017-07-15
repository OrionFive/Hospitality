using Harmony;
using Verse;
using Verse.AI.Group;

namespace Hospitality.Harmony
{
    internal static class Pawn_Patch
    {
        [HarmonyPatch(typeof(Pawn), "GiveSoldThingToPlayer")]
        public class GiveSoldThingToPlayer
        {
            [HarmonyPrefix]
            internal static bool Prefix(Pawn __instance, Thing toGive)
            {
                if (!__instance.IsGuest()) return true;
                var lord = __instance.GetLord();
                if (lord == null) return true;
                var toil = lord.CurLordToil as LordToil_VisitPoint;
                if (toil == null) return true;

                // We got a proper guest
                toil.OnPlayerBoughtItem(toGive);
                return true;
            }
        }


        [HarmonyPatch(typeof(Pawn), "GiveSoldThingToTrader")]
        public class GiveSoldThingToTrader
        {
            [HarmonyPrefix]
            internal static bool Prefix(Pawn __instance, Thing toGive)
            {
                if (!__instance.IsGuest()) return true;
                var lord = __instance.GetLord();
                if (lord == null) return true;
                var toil = lord.CurLordToil as LordToil_VisitPoint;
                if (toil == null) return true;

                // We got a proper guest
                toil.OnPlayerSoldItem(toGive);
                return true;
            }
        }
    }
}