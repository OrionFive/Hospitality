using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Hospitality.Harmony
{
    /// <summary>
    /// Trying to find out why pawns get compressed away
    /// </summary>
    /*public class WorldPawns_Patch
    {
        [HarmonyPatch(typeof(WorldPawns), "DiscardPawn")]
        public class DiscardPawn
        {
            [HarmonyPrefix]
            public static bool Prefix(Pawn p, bool silentlyRemoveReferences)
            {
                if (!p.Discarded && p.def.race?.Humanlike == true)
                    Log.Error($"About to discard pawn {p.NameFullColored} ({p.ThingID}). Silently? {silentlyRemoveReferences}");
                if (p.def.race?.Humanlike == true && p.GetComp<CompGuest>()?.lastBedCheckTick != 0)
                    Log.Message($"{p.NameFullColored} was probably a guest.");
                return true;
            }
        }
    }*/
}
