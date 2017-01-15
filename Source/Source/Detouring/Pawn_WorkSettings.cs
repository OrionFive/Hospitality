using System.Linq;
using System.Reflection;
using Verse;
using Source = RimWorld.Pawn_WorkSettings;

namespace Hospitality.Detouring
{
    public static class Pawn_WorkSettings
    {
        /// <summary>
        /// Disable work types that the pawn can't do. This is a nasty fix for RimWorld not supporting adding new jobs to an existing savegame. :[
        /// </summary>
        [Detour(typeof (Source), bindingFlags=BindingFlags.Instance|BindingFlags.Public)]
        public static void ExposeData(this Source _this)
        {
            var fieldPriorities = typeof(RimWorld.Pawn_WorkSettings).GetField("priorities", BindingFlags.Instance|BindingFlags.NonPublic);
            var fieldPawn = typeof (RimWorld.Pawn_WorkSettings).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic);
            var priorities = (DefMap<WorkTypeDef, int>)fieldPriorities.GetValue(_this);

            Scribe_Deep.LookDeep(ref priorities, "priorities", new object[0]); // BASE

            // Added: Make checks?
            if (Scribe.mode == LoadSaveMode.PostLoadInit && priorities != null)
            {
                CheckForRemovedOrAdded(_this, ref priorities, fieldPriorities);

                //CheckForDisabledTypes(_this, (Pawn) fieldPawn.GetValue(_this));
            }

            // Apply
            fieldPriorities.SetValue(_this, priorities);

        }

        private static void CheckForRemovedOrAdded(Source _this, ref DefMap<WorkTypeDef, int> priorities, FieldInfo fieldPriorities)
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
                fieldPriorities.SetValue(_this, priorities);
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
                fieldPriorities.SetValue(_this, priorities);
            }
        }

        private static void CheckForDisabledTypes(Source _this, Pawn pawn)
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