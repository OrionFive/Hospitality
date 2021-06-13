using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace Hospitality
{
    public abstract class Alert_Guest : Alert
    {
        protected List<Pawn> affectedPawnsResult = new List<Pawn>();
        protected string explanationKey;

        protected abstract List<Pawn> FindAffectedPawns();
        private readonly List<Pawn> affectedPawnCache = new List<Pawn>();
        private protected abstract int Hash { get; }

        public override string GetLabel()
        {
            int count = affectedPawnCache.Count;
            string label = base.GetLabel();
            return count > 1 ? $"{label} x{count:D}" : label;
        }

        public override AlertReport GetReport()
        {
            if ((Find.TickManager.TicksGame + Hash) % 250 == 0)
            {
                affectedPawnCache.Clear();
                affectedPawnCache.AddRange(FindAffectedPawns());
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
