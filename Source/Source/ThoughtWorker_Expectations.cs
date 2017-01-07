using System;
using RimWorld;
using Verse;

namespace Hospitality
{
    /// <summary>
    /// Loaded via xml instead of vanilla worker. Added so guests can have this thought
    /// </summary>
    public class ThoughtWorker_Expectations : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            try
            {
                if (pawn == null) return ThoughtState.Inactive;
                if (pawn.thingIDNumber == 0) return ThoughtState.Inactive; // What do you know!!!

                if (Current.ProgramState != ProgramState.Playing)
                {
                    return ThoughtState.Inactive;
                }
                var isGuest = pawn.IsGuest();
                if (pawn.Faction != Faction.OfPlayer && !isGuest) // Added guest check
                {
                    return ThoughtState.ActiveAtStage(3);
                }
                float wealthTotal = pawn.MapHeld.wealthWatcher.WealthTotal * (isGuest ? 2 : 1);
                if (wealthTotal < 10000f)
                {
                    return ThoughtState.ActiveAtStage(3);
                }
                if (wealthTotal < 50000f)
                {
                    return ThoughtState.ActiveAtStage(2);
                }
                if (wealthTotal < 150000f)
                {
                    return ThoughtState.ActiveAtStage(1);
                }
                if (wealthTotal < 300000f)
                {
                    return ThoughtState.ActiveAtStage(0);
                }
                return ThoughtState.Inactive;
            }
            catch(Exception e)
            {
                Log.Warning(e.Message);
                return ThoughtState.Inactive;
            }
        }
    }
}