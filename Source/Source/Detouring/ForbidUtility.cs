using System.Reflection;
using RimWorld;
using Verse;

namespace Hospitality.Detouring
{
    internal static class ForbidUtility
    {
        // So guests will care
        [Detour(typeof(RimWorld.ForbidUtility), bindingFlags = BindingFlags.NonPublic| BindingFlags.Static)]
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