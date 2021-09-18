using Hospitality.Utilities;
using RimWorld;
using Verse;

namespace Hospitality
{
    /// <summary>
    /// This extension of the vanilla ThoughtWorker adds a way to cache the result for a set interval
    /// </summary>
    public class ThoughtWorkerCached : ThoughtWorker
    {
        public virtual int ThoughtCacheInterval { get; }

        /// <summary>
        /// Override <see cref="ShouldCache"/> and <see cref="GetStateToCache"/> instead.
        /// </summary>
        public sealed override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            if (!ShouldCache(pawn)) return false;

            if (ThoughtResultCache.NeedsNewState(pawn, this, out var existingState))
            {
                var state = GetStateToCache(pawn);
                ThoughtResultCache.CacheThoughtResult(pawn, this, state);
                return state;
            }
            return existingState;
        }

        public virtual bool ShouldCache(Pawn pawn) => true;

        public virtual ThoughtState GetStateToCache(Pawn pawn)
        {
            return base.CurrentStateInternal(pawn);
        }
    }
}
