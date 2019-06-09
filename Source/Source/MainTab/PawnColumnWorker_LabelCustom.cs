using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Hospitality.MainTab
{
    public class PawnColumnWorker_LabelCustom : PawnColumnWorker_Label
    {
        private int guestCountCached;
        private int bedCountCached;

        private float lastTimeCached;
        private Map currentMap;

        public override void DoHeader(Rect rect, PawnTable table)
        {
            base.DoHeader(rect, table);

            if (Time.unscaledTime > lastTimeCached + 2 || Find.CurrentMap != currentMap)
            {
                guestCountCached = Find.CurrentMap.lordManager.lords.Where(l => l?.ownedPawns != null)
                    .SelectMany(l => l.ownedPawns).Count(p => p.IsGuest());
                bedCountCached =  Find.CurrentMap.GetGuestBeds().Count();
                lastTimeCached = Time.unscaledTime;
                currentMap = Find.CurrentMap;
            }

            Text.Font = DefaultHeaderFont;
            GUI.color = guestCountCached > bedCountCached ? Color.red : DefaultHeaderColor;
            Text.Anchor = TextAnchor.LowerLeft;
            Rect label = rect;
            label.y += 3f;
            Widgets.Label(label, "BedsFilled".Translate(guestCountCached, bedCountCached));
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }
    }
}
