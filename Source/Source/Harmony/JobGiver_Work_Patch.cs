using System.Reflection;
using Harmony;
using RimWorld;
using UnityEngine;
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
                if (!pawn.IsGuest()) return true;
                float score;
                if (!pawn.GetVisitScore(out score)) return false;

                if (pawn.Faction.GoodwillWith(Faction.OfPlayer) + score*100 < Rand.ValueSeeded(pawn.thingIDNumber ^ 3436436)*75) return false;
                var skill = pawn.skills.AverageOfRelevantSkillsFor(giver.def.workType);
                var canDo = !giver.ShouldSkip(pawn) && giver.MissingRequiredCapacity(pawn) == null && skill > 0;
                if (!canDo) return false;

                var passion = pawn.skills.MaxPassionOfRelevantSkillsFor(giver.def.workType);
                var wantsTo = giver.def.emergency || (skill >= 6 && passion == Passion.Major) || skill >= 9;
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
                if (pawn.IsGuest())
                {
                    var priorities = _fieldPriorities.GetValue(pawn.workSettings);
                    if (priorities == null) pawn.workSettings.EnableAndInitialize();
                }
                return true;
            }
        }
    }
}