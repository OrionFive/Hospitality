using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Hospitality
{
    public class StatWorker_PleaseGuestChance : StatWorker
    {
        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            var pawn = req.Thing as Pawn;
            if (pawn == null || pawn.story==null) return 0;

            var isAbrasive = pawn.story.traits.HasTrait(TraitDefOf.Abrasive);
            var abrasiveFactor = isAbrasive ? 0.65f : 1f;
            
            var hasNoSocialSkill = pawn.skills.skills.All(s => s.def != SkillDefOf.Social);
            if (hasNoSocialSkill) return abrasiveFactor*0.25f;

            return abrasiveFactor*base.GetValueUnfinalized(req, applyPostProcess);
        }
        public override string GetExplanation(StatRequest req, ToStringNumberSense numberSense)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetExplanation(req, numberSense));

            var pawn = req.Thing as Pawn;
            if (pawn == null || pawn.story == null) return stringBuilder.ToString();

            var isAbrasive = pawn.story.traits.HasTrait(TraitDefOf.Abrasive);
            var abrasiveFactor = isAbrasive ? 0.65f : 1f;

            if (isAbrasive)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(string.Format("    {0}: x{1}", TraitDefOf.Abrasive.degreeDatas[0].label.CapitalizeFirst(), abrasiveFactor.ToStringPercent()));
            }
            return stringBuilder.ToString();
        }
    }
}