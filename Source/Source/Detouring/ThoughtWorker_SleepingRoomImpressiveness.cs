using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace Hospitality.Detouring
{
    /// <summary>
    /// Added that guests can have this thought
    /// </summary>
    public static class ThoughtWorker_SleepingRoomImpressiveness
    {
        [Detour(typeof(RimWorld.ThoughtWorker_SleepingRoomImpressiveness), bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance)]
        public static ThoughtState CurrentStateInternal(RimWorld.ThoughtWorker_SleepingRoomImpressiveness _this, Pawn p)
        {
            if (p == null) return ThoughtState.Inactive; // Added

            // BASE
            if (p.story.traits.HasTrait(TraitDefOf.Ascetic))
            {
                return ThoughtState.Inactive;
            }
            #region Added
            if (p.IsGuest())
            {
                var beds = p.GetGuestBeds();
                if(!beds.Any()) return ThoughtState.Inactive;

                return CheckRoom(_this, beds.MinBy(b=>b.Position.DistanceToSquared(p.PositionHeld)).GetRoom());
            }
            #endregion

            // Sorta base
            if (p.ownership == null || p.ownership.OwnedBed == null)
            {
                return ThoughtState.Inactive;
            }

            // Refactored into function for double use
            return CheckRoom(_this, p.ownership.OwnedBed.GetRoom());
        }

        private static ThoughtState CheckRoom(RimWorld.ThoughtWorker_SleepingRoomImpressiveness _this, Room room)
        {
            // BASE
            if (room == null)
            {
                return ThoughtState.Inactive;
            }
            int scoreStageIndex = RoomStatDefOf.Impressiveness.GetScoreStageIndex(room.GetStat(RoomStatDefOf.Impressiveness));
            if (_this.def.stages[scoreStageIndex] != null)
            {
                return ThoughtState.ActiveAtStage(scoreStageIndex);
            }
            return ThoughtState.Inactive;
        }
    }
}