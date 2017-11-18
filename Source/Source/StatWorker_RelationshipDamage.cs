using System.Text;
using RimWorld;
using Verse;

namespace Hospitality
{
    public class StatWorker_RelationshipDamage : StatWorker
    {
        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            var factor = req.HasThing && req.Thing is Pawn ? PriceUtility.PawnQualityPriceFactor((Pawn) req.Thing) : 0;
            return factor*stat.defaultBaseValue-stat.defaultBaseValue/6;
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            var stringBuilder = new StringBuilder();
            if (!req.HasThing || !(req.Thing is Pawn)) return base.GetExplanationUnfinalized(req, numberSense);

            stringBuilder.AppendLine("StatsReport_BaseValue".Translate());
            float statValueAbstract = stat.defaultBaseValue;
            stringBuilder.AppendLine("    " + stat.ValueToString(statValueAbstract, numberSense));
            
            var pawn = req.Thing as Pawn;
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(string.Format("{0}: x{1} {2}",
                "StatsReport_CharacterQuality".Translate(), PriceUtility.PawnQualityPriceFactor(pawn).ToStringPercent(), -stat.defaultBaseValue/6));
            return stringBuilder.ToString();
        }
    }
}