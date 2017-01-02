using System;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Hospitality
{
    public class StatWorker_RecruitEffectivity : StatWorker
    {
        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            var pawn = req.Thing as Pawn;
            if (pawn == null || pawn.story == null) return 0;
            return stat.defaultBaseValue + pawn.skills.GetSkill(SkillDefOf.Social).level/8f;
        }

        public override string GetExplanation(StatRequest req, ToStringNumberSense numberSense)
        {
            if (!req.HasThing || !(req.Thing is Pawn)) return base.GetExplanation(req, numberSense);

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("StatsReport_BaseValue".Translate());
            float statValueAbstract = stat.defaultBaseValue;
            stringBuilder.AppendLine("    " + stat.ValueToString(statValueAbstract, numberSense));

            var pawn = req.Thing as Pawn;
            
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("StatsReport_Skills".Translate());

            int level = pawn.skills.GetSkill(SkillDefOf.Social).level;
            stringBuilder.AppendLine(string.Format("    {0} ({1}): +{2}", SkillDefOf.Social.LabelCap, level, (level / 8f).ToStringDecimalIfSmall()));

            return stringBuilder.ToString();
        }
    }
}