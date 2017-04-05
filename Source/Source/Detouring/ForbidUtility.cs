using System.Reflection;
using HugsLib.Source.Detour;
using RimWorld;
using Verse;

namespace Hospitality.Detouring
{
    internal static class ForbidUtility
    {
        // So guests will care
        [DetourMethod(typeof(RimWorld.ForbidUtility), "CaresAboutForbidden")]
        public static bool CaresAboutForbidden(Pawn pawn, bool cellTarget)
        {
            return !pawn.InMentalState && AddedFactionCheck(pawn) && (!cellTarget || !ThinkNode_ConditionalShouldFollowMaster.ShouldFollowMaster(pawn));
        }

        private static bool AddedFactionCheck(Pawn pawn)
        {
            return pawn.HostFaction == null || pawn.IsGuest();
        }
    }
}