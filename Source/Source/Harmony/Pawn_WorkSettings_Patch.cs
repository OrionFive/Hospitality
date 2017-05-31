using System.Linq;
using Harmony;
using RimWorld;
using Verse;
using Source = RimWorld.Pawn_WorkSettings;

namespace Hospitality.Harmony
{
    public static class Pawn_WorkSettings_Patch
    {
        /// <summary>
        /// Disable work types that the pawn can't do. This is a nasty fix for RimWorld not supporting adding new jobs to an existing savegame. :[
        /// </summary>
        [HarmonyPatch(typeof(Source), "ExposeData")]
        public class ExposeData
        {
            [HarmonyPostfix] 
            public static void Postfix(Pawn_WorkSettings __instance)
            {
                var fieldPriorities = Traverse.Create(__instance).Field("priorities");
                var pawn = Traverse.Create(__instance).Field("pawn").GetValue();
                var priorities = (DefMap<WorkTypeDef, int>) fieldPriorities.GetValue();

                // Added: Make checks?
                if (Scribe.mode == LoadSaveMode.PostLoadInit && priorities != null)
                {
                    CheckForRemovedOrAdded(ref priorities, fieldPriorities);

                    CheckForDisabledTypes(__instance, (Pawn) pawn);
                }

                // Apply
                fieldPriorities.SetValue(priorities);

            }


            private static void CheckForRemovedOrAdded(ref DefMap<WorkTypeDef, int> priorities, Traverse fieldPriorities)
            {
                var newDefCount = DefDatabase<WorkTypeDef>.AllDefs.Count();
                // Added
                if (priorities.Count < newDefCount)
                {
                    var newMap = new DefMap<WorkTypeDef, int>();

                    for (int i = 0; i < priorities.Count; i++)
                    {
                        newMap[i] = priorities[i];
                    }
                    // Apply
                    priorities = newMap;
                    fieldPriorities.SetValue(priorities);
                }
                // Removed
                else if (priorities.Count > newDefCount)
                {
                    var newMap = new DefMap<WorkTypeDef, int>();

                    for (int i = 0; i < newDefCount; i++)
                    {
                        newMap[i] = priorities[i];
                    }
                    // Apply
                    priorities = newMap;
                    fieldPriorities.SetValue(priorities);
                }
            }

            private static void CheckForDisabledTypes(Pawn_WorkSettings _this, Pawn pawn)
            {
                foreach (var workTypeDef in WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder)
                {
                    if (pawn.story.WorkTypeIsDisabled(workTypeDef))
                    {
                        _this.Disable(workTypeDef);
                    }
                }
            }
        }
    }
}