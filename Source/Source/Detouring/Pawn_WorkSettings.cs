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

            Check(_this, priorities, fieldPawn); // Added

            // Apply
            fieldPriorities.SetValue(_this, priorities);
        }

        private static void Check(Source _this, DefMap<WorkTypeDef, int> priorities, FieldInfo fieldPawn)
        {
            // Check
            if (Scribe.mode == LoadSaveMode.PostLoadInit && priorities != null)
            {
                var pawn = (Pawn) fieldPawn.GetValue(_this);
                foreach (var workTypeDef in WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder)
                {
                    if (pawn.story.WorkTypeIsDisabled(workTypeDef))
                    {
                        _this.SetPriority(workTypeDef, 0);
                    }
                }
            }
        }
    }
}