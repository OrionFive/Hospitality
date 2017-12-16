using System.Reflection;
using Harmony;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
    public class JobGiver_Work_Patch
    {
        /// <summary>
        /// Allow guests to do work they enjoy and are reasonably good at
        /// </summary>
        [HarmonyPatch(typeof(JobGiver_Work), "PawnCanUseWorkGiver")]
        public class PawnCanUseWorkGiver
        {
            public static bool Prefix(ref bool __result, Pawn pawn, WorkGiver giver)
            {
                if (Settings.disableWork) return true;
                if (!pawn.IsGuest()) return true;
                float score;
                if (!pawn.GetVisitScore(out score)) return false;

                var passion = pawn.skills.MaxPassionOfRelevantSkillsFor(giver.def.workType);
                float passionBonus = passion == Passion.Major ? 40 : passion == Passion.Minor ? 20 : 0;

                var desireToHelp = pawn.Faction.GoodwillWith(Faction.OfPlayer) + passionBonus + score*100;
                if (desireToHelp < Rand.ValueSeeded(pawn.thingIDNumber ^ 3436436)*75) return false;

                var skill = pawn.skills.AverageOfRelevantSkillsFor(giver.def.workType);

                var canDo = !giver.ShouldSkip(pawn) && giver.MissingRequiredCapacity(pawn) == null && skill > 0;
                if (!canDo) return false;

                var wantsTo = giver.def.emergency || skill >= Settings.minGuestWorkSkill.Value;
                if (!wantsTo) return false;
                
                __result = true;
                return false;
            }
        }

        /// <summary>
        /// Make sure they have workSettings.priorities before they attempt to do work
        /// </summary>
        [HarmonyPatch(typeof(JobGiver_Work), "TryIssueJobPackage")]
        public class TryIssueJobPackage
        {
            private static FieldInfo _fieldPriorities = typeof(Pawn_WorkSettings).GetField("priorities", BindingFlags.NonPublic | BindingFlags.Instance);

            public static bool Prefix(Pawn pawn)
            {
                if (!Settings.disableWork && pawn.IsGuest())
                {
                    var priorities = _fieldPriorities.GetValue(pawn.workSettings);
                    if (priorities == null) pawn.workSettings.EnableAndInitialize();
                }
                return true;
            }
        }
    }
}