﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Hospitality.Utilities
{
    /// <summary>
    /// This cache utility saves all results of <see cref="ThoughtWorkerCached"/> and manages whether it needs an update or not
    /// </summary>
    public static class ThoughtResultCache
    {
        //We save both the last result and the last tick it got cached at
        private static Dictionary<int, ThoughtState> cachedStates = new Dictionary<int, ThoughtState>();
        private static Dictionary<int, int> cachedTick = new Dictionary<int, int>();

        public static void CacheThoughtResult(Pawn forPawn, ThoughtWorkerCached worker, ThoughtState result)
        {
            //For unique caching we get a mix of both the pawn and the worker's hash code
            int hashMix = CombinedHash(forPawn, worker);
            int curTick = Find.TickManager.TicksGame;

            if (!cachedStates.ContainsKey(hashMix))
            {
                cachedStates.Add(hashMix, result);
                cachedTick.Add(hashMix, curTick);
                return;
            }
            cachedStates[hashMix] = result;
            cachedTick[hashMix] = curTick;
        }

        //If the time between current tick and last cache tick is greater than the interval defined in the worker, we request a new state
        internal static bool NeedsNewState(Pawn forPawn, ThoughtWorkerCached worker, out ThoughtState existingState)
        {
            int hashMix = CombinedHash(forPawn, worker);
            int curTick = Find.TickManager.TicksGame;

            existingState = cachedStates.TryGetValue(hashMix, false);

            if (cachedTick.TryGetValue(hashMix, out int lastCachedTick))
            {
                return curTick - lastCachedTick > worker.ThoughtCacheInterval;
            }
            return true;
        }

        internal static int CombinedHash(object first, object second)
        {
            int hash = 17;
            hash = hash * 31 + first.GetHashCode();
            hash = hash * 31 + second.GetHashCode();
            return hash;
        }
    }
}
