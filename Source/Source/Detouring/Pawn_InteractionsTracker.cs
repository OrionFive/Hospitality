using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using Source = RimWorld.Pawn_InteractionsTracker;

namespace Hospitality.Detouring
{
    /// <summary>
    /// Allow colonists to talk to guests randomly
    /// </summary>
    internal static class Pawn_InteractionsTracker
    {
        [Detour(typeof (Source), bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance)]
        public static bool TryInteractRandomly(this Source _this)
        {
            var pawn =
                (Pawn)
                    typeof (Source).GetField("pawn",
                        BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_this);

            if (!IsInteractable(pawn)) return false;
            var workingList = (List<Pawn>)typeof(Source).GetField("workingList", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null); // Had to add

            // BASE
            if (_this.InteractedTooRecentlyToInteract())
            {
                return false;
            }
            if (!InteractionUtility.CanInitiateRandomInteraction(pawn))
            {
                return false;
            }
            var collection = pawn.MapHeld.mapPawns.AllPawnsSpawned.Where(IsInteractable).InRandomOrder(); // Added
            workingList.Clear();
            workingList.AddRange(collection);
            workingList.Shuffle<Pawn>();
            List<InteractionDef> allDefsListForReading = DefDatabase<InteractionDef>.AllDefsListForReading;
            for (int i = 0; i < workingList.Count; i++)
            {
                Pawn p = workingList[i];
                if (p != pawn && CanInteractNowWith(pawn, p) && InteractionUtility.CanReceiveRandomInteraction(p) && !pawn.HostileTo(p))
                {
                    InteractionDef intDef;
                    if (allDefsListForReading.TryRandomElementByWeight((InteractionDef x) => x.Worker.RandomSelectionWeight(pawn, p), out intDef))
                    {
                        if (_this.TryInteractWith(p, intDef))
                        {
                            return true;
                        }
                        Log.Error(pawn + " failed to interact with " + p);
                    }
                }
            }
            return false;
        }

        private static bool IsInteractable(Pawn pawn) // Added
        {
            return pawn != null && !pawn.Downed && pawn.RaceProps.Humanlike && pawn.relations != null
                   && pawn.story != null && pawn.story.traits != null;
        }

        private static bool CanInteractNowWith(Pawn pawn, Pawn recipient) // Had to add, copy
        {
            return recipient.Spawned
                   && ((pawn.Position - recipient.Position).LengthHorizontalSquared <= 36.0
                       && InteractionUtility.CanInitiateInteraction(pawn)
                       && (InteractionUtility.CanReceiveInteraction(recipient)
                           && GenSight.LineOfSight(pawn.Position, recipient.Position, pawn.MapHeld, true)));
        }
    }
}