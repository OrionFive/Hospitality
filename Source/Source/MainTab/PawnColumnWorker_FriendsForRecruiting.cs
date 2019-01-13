using RimWorld;
using UnityEngine;
using Verse;

namespace Hospitality.MainTab
{
    public class PawnColumnWorker_FriendsForRecruiting : PawnColumnWorker_Text
    {
        private Color enoughFriendsColor = new Color(0, 1, 0, 0.2f);

        // Storing it just long enough to use it twice
        protected internal int friendsShortCache;
        protected internal int friendsRequiredShortCache;

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if (!pawn.IsGuest()) return;

            // Store cache
            friendsShortCache = pawn.GetFriendsInColony();
            friendsRequiredShortCache = GuestUtility.FriendsRequired(pawn.MapHeld) + pawn.GetEnemiesInColony();

            base.DoCell(rect, pawn, table);

            // Use cache
            if (friendsShortCache >= friendsRequiredShortCache)
            {
                var rect2 = rect;
                rect2.width -= 10;
                Widgets.DrawBoxSolid(rect2, enoughFriendsColor);
            }
        }

        protected override string GetTextFor(Pawn pawn)
        {
            // Use cache
            return $"{friendsShortCache}/{friendsRequiredShortCache}";
        }

        public override int Compare(Pawn a, Pawn b)
        {
            return GetValueToCompare(a).CompareTo(GetValueToCompare(b));
        }

        private int GetValueToCompare(Pawn pawn)
        {
            // Changed check
            if (!pawn.IsGuest())
            {
                return -2147483648;
            }

            if (friendsRequiredShortCache == 0) return -2147483648;
            return (int)(100f * friendsShortCache / friendsRequiredShortCache);
        }
    }
}
