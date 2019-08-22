using Harmony;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
    /// <summary>
    /// When a rescued guest leaves the map, mark him as rescued for the reward
    /// </summary>
    public class Faction_Patch
    {
        [HarmonyPatch(typeof(Faction), "Notify_MemberExitedMap")]
        public class Notify_MemberExitedMap
        {
            [HarmonyPrefix]
            public static void Prefix(Pawn member, ref bool free)
            {
                var compGuest = member.GetComp<CompGuest>();
                if (compGuest == null || !compGuest.rescued || member.guest == null || PawnUtility.IsTravelingInTransportPodWorldObject(member)) return;

                free = true;
                Traverse.Create(member.guest).Field("hostFactionInt").SetValue(Faction.OfPlayer);
                compGuest.rescued = false; // Turn back off
            }
        }
    }
}
