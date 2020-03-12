using HarmonyLib;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
    public class Toils_LayDown_Patch
    {
        /// <summary>
        /// So guests can think about their bedroom
        /// </summary>
        [HarmonyPatch(typeof(Toils_LayDown), "ApplyBedThoughts")]
        public class ApplyBedThoughts
        {
            [HarmonyPrefix]
            public static bool Replacement(Pawn actor)
            {
                if (actor.needs.mood == null) return false;

                Building_Bed building_Bed = actor.CurrentBed();
                actor.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.SleptInBedroom);
                actor.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.SleptInBarracks);
                actor.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.SleptOutside);
                actor.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.SleptOnGround);
                actor.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.SleptInCold);
                actor.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.SleptInHeat);
                if (actor.GetRoom(RegionType.Set_Passable).PsychologicallyOutdoors)
                {
                    actor.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.SleptOutside, null);
                }
                if (building_Bed == null || building_Bed.CostListAdjusted().Count == 0)
                {
                    actor.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.SleptOnGround, null);
                }
                if (actor.AmbientTemperature < actor.def.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin, null))
                {
                    actor.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.SleptInCold, null);
                }
                if (actor.AmbientTemperature > actor.def.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax, null))
                {
                    actor.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.SleptInHeat, null);
                }
                if (building_Bed != null && AddedBedIsOwned(actor, building_Bed) && !building_Bed.ForPrisoners && !actor.story.traits.HasTrait(TraitDefOf.Ascetic))
                {
                    ThoughtDef thoughtDef = null;
                    // ADDED:
                    if (building_Bed.GetRoom(RegionType.Set_Passable).Role == BedUtility.roleDefGuestRoom)
                    {
                        thoughtDef = building_Bed.GetRoom().OnlyOneBed() ? ThoughtDefOf.SleptInBedroom : ThoughtDefOf.SleptInBarracks;
                    } ////
                    else if (building_Bed.GetRoom(RegionType.Set_Passable).Role == RoomRoleDefOf.Bedroom)
                    {
                        thoughtDef = ThoughtDefOf.SleptInBedroom;
                    }
                    else if (building_Bed.GetRoom(RegionType.Set_Passable).Role == RoomRoleDefOf.Barracks)
                    {
                        thoughtDef = ThoughtDefOf.SleptInBarracks;
                    }
                    if (thoughtDef != null)
                    {
                        int scoreStageIndex = RoomStatDefOf.Impressiveness.GetScoreStageIndex(building_Bed.GetRoom(RegionType.Set_Passable).GetStat(RoomStatDefOf.Impressiveness));
                        if (thoughtDef.stages[scoreStageIndex] != null)
                        {
                            actor.needs.mood.thoughts.memories.TryGainMemory(ThoughtMaker.MakeThought(thoughtDef, scoreStageIndex), null);
                        }
                    }
                }
                return false;
            }

            private static Building_Bed GetGuestBed(Pawn pawn)
            {
                var compGuest = pawn.GetComp<CompGuest>();
                return compGuest?.bed;
            }

            // Added
            private static bool AddedBedIsOwned(Pawn pawn, Building_Bed building_Bed)
            {
                return pawn.IsGuest() 
                    ? GetGuestBed(pawn) == building_Bed 
                    : building_Bed == pawn.ownership.OwnedBed;
            }
        }
    }
}