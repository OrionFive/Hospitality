using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public sealed override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!ShouldCache(p)) return false;

            if (ThoughtResultCache.NeedsNewState(p, this, out ThoughtState existingState))
            {
                var state = GetStateToCache(p);
                ThoughtResultCache.CacheThoughtResult(p, this, state);
                return state;
            }
            return existingState;
        }

        public virtual bool ShouldCache(Pawn forPawn)
        {
            return true;
        }

        public virtual ThoughtState GetStateToCache(Pawn p)
        {
            return base.CurrentStateInternal(p);
        }
    }
}
