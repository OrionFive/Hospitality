using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Hospitality
{
    public class Gizmo_GuestBedStats : Gizmo
    {
        private static readonly Color GuestBedStatsColor = new Color(0.75f, 0.75f, 0.75f);
        private Building_GuestBed building;

        public Gizmo_GuestBedStats(Building_GuestBed building)
        {
            this.building = building;
            order = -100f;
        }

        private Room Room => Gizmo_RoomStats.GetRoomToShowStatsFor(building);

        public override float GetWidth(float maxWidth)
        {
            return Mathf.Min(200f, maxWidth);
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth)
        {
            var room = Room;
            if (room == null) return new GizmoResult(GizmoState.Clear);

            Rect rectWindow = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Widgets.DrawWindowBackground(rectWindow);
            Text.WordWrap = false;
            GUI.BeginGroup(rectWindow);
            Rect rectInner = rectWindow.AtZero().ContractedBy(10f);
            Text.Font = GameFont.Small;
            Rect rectTitle = new Rect(rectInner.x, rectInner.y - 2f, rectInner.width, 100f);
            var title = building.Stats.title;
            Widgets.Label(rectTitle, title.Truncate(rectTitle.width));
            float y = (float) (rectInner.y + (double) Text.LineHeight + Text.SpaceBetweenLines + 7.0);
            GUI.color = GuestBedStatsColor;
            Text.Font = GameFont.Tiny;
            IEnumerable<TaggedString> stats = building.Stats.textAsArray;
            foreach (var label in stats)
            {
                Rect rectStat = new Rect(rectInner.x, y, rectInner.width, 100f);
                Widgets.Label(rectStat, label.Truncate(rectStat.width));
                y += Text.LineHeight + Text.SpaceBetweenLines;
            }

            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            GUI.EndGroup();
            Text.WordWrap = true;
            GenUI.AbsorbClicksInRect(rectWindow);
            if (!Mouse.IsOver(rectWindow))
                return new GizmoResult(GizmoState.Clear);
            Rect windowRect = EnvironmentStatsDrawer.GetWindowRect(false, true);
            Find.WindowStack.ImmediateWindow(74975, windowRect, WindowLayer.Super, () => {
                float curY = 18f;
                EnvironmentStatsDrawer.DoRoomInfo(room, ref curY, windowRect);
            });
            return new GizmoResult(GizmoState.Mouseover);
        }
    }
}
