using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Hospitality
{
    public abstract class Alert_Guest : Alert
    {
        protected List<Pawn> affectedPawnCache = new List<Pawn>();
        protected string explanationKey;
        
        protected abstract void UpdateAffectedPawnsCache();
        private protected abstract int Hash { get; }
        private float nextCacheUpdate;

        public override string GetLabel()
        {
            int count = affectedPawnCache.Count;
            string label = base.GetLabel();
            return count > 1 ? $"{label} x{count:D}" : label;
        }

        public override AlertReport GetReport()
        {
            if (Time.realtimeSinceStartup >= nextCacheUpdate)
            {
                UpdateAffectedPawnsCache();
                nextCacheUpdate = Time.realtimeSinceStartup + 1 + 0.01f * (Hash % 25);
            }
            return affectedPawnCache.Any() ? AlertReport.CulpritsAre(affectedPawnCache) : AlertReport.Inactive;
        }

        public override TaggedString GetExplanation()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (Pawn affectedPawn in affectedPawnCache)
                stringBuilder.AppendLine("  - " + affectedPawn.NameShortColored.Resolve());
            return explanationKey.Translate(stringBuilder.ToString());
        }
    }
}
