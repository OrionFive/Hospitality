using System;
using System.Reflection;
using HugsLib.Source.Detour;
using RimWorld;
using Verse;

namespace Hospitality.Detouring
{
    /// <summary>
    /// Rescued pawns get marked as rescued for follow up
    /// </summary>
    public static class Pawn_RelationsTracker
    {
        [DetourMethod(typeof(RimWorld.Pawn_RelationsTracker), "Notify_RescuedBy")]
        public static void Notify_RescuedBy(this RimWorld.Pawn_RelationsTracker _this, Pawn rescuer)
        {
            if (rescuer.RaceProps.Humanlike && _this.canGetRescuedThought)
            {
                var pawn = _this.GetType().GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_this) as Pawn; // Had to add

                pawn.needs.mood.thoughts.memories.TryGainMemoryThought(ThoughtDefOf.RescuedMe, rescuer);
                _this.canGetRescuedThought = false;

#region Added
                var compGuest = pawn.GetComp<CompGuest>();
                if (compGuest != null)
                {
                    compGuest.OnRescued();
                }

                // Psychology compatibility
                ThoughtDef kp = DefDatabase<ThoughtDef>.GetNamedSilentFail("RescuedBleedingHeart");
                if (kp != null)
                {
                    rescuer.needs.mood.thoughts.memories.TryGainMemoryThought(kp, pawn);
                }
#endregion
            }
        }

        [DetourFallback("Notify_RescuedBy")]
        public static void DetourFallbackHandler(MemberInfo attemptedDestination, MethodInfo existingDestination, Exception detourException)
        {
            if (existingDestination != null)
            {
                LongEventHandler.QueueLongEvent(() =>
                {
                    ThoughtDef kp = DefDatabase<ThoughtDef>.GetNamedSilentFail("RescuedBleedingHeart");
                    if (kp != null) // Psychology is loaded
                    {
                        Find.WindowStack.Add(new Dialog_MessageBox("Hospitality: Please load the Psychology mod after Hospitality to ensure proper functioning."));
                    }
                    else 
                    {
                        Log.Error(string.Format("Hospitality: 'Pawn_RelationsTracker.Notify_RescuedBy' was already detoured to {0}", existingDestination.Name));
                    }  
                }, null, false, null);
            }
        }
    }
}
