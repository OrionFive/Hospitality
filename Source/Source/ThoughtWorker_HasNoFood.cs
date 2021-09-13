using RimWorld;
using Verse;

namespace Hospitality
{
    /// <summary>
    /// Loaded via xml. Added so guests are upset when they have nothing to eat.
    /// </summary>
    public class ThoughtWorker_HasNoFood : ThoughtWorkerCached
    {
        public override int ThoughtCacheInterval => GenTicks.TickLongInterval;

        public override ThoughtState GetStateToCache(Pawn p)
        {
            var flag = Utilities.FoodUtility.GuestCanSatisfyFoodNeed(p) ? ThoughtState.Inactive : ThoughtState.ActiveDefault;
            return flag;
        }

        public override bool ShouldCache(Pawn p)
        {
            if (p == null) return false;
            if (p.thingIDNumber == 0) return false; // What do you know!!!

            if (Current.ProgramState != ProgramState.Playing)
            {
                return false;
            }
            if (!p.IsArrivedGuest(out var compGuest)) return false;
            if (compGuest == null) return false;
            if (!compGuest.arrived) return false;
            return true;
        }
    }
}
