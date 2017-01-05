using System.Reflection;
using RimWorld;
using Verse;
using Source = RimWorld.Pawn_GuestTracker;

namespace Hospitality.Detouring
{
    internal static class Pawn_GuestTracker
    {
        [Detour(typeof(Source), bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance)]
        internal static void Notify_PawnUndowned(Source _this)
        {
            var pawn = (Pawn)typeof(Source).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_this);

            if (pawn.RaceProps.Humanlike && _this.HostFaction == Faction.OfPlayer && (pawn.Faction == null || pawn.Faction.def.rescueesCanJoin) && !_this.IsPrisoner)
            {
#region CHANGED
                bool hostileEnvironment = !pawn.SafeTemperatureRange().Includes(pawn.Map.mapTemperature.OutdoorTemp) || pawn.Map.mapConditionManager.ConditionIsActive(MapConditionDefOf.ToxicFallout);
                if (hostileEnvironment || GuestUtility.WillRescueJoin(pawn))
#endregion
                {
                    pawn.SetFaction(Faction.OfPlayer);
                    Messages.Message("MessageRescueeJoined".Translate(new object[]
                    {
                        pawn.LabelShort
                    }).AdjustedFor(pawn), pawn, MessageSound.Benefit);
                }
            }
        }
    }
}