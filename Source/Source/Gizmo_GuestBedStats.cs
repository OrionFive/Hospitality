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
        private readonly Building_GuestBed bed;

        public Gizmo_GuestBedStats(Building_GuestBed bed)
        {
            this.bed = bed;
            order = -100f;
        }

        private Room Room => Gizmo_RoomStats.GetRoomToShowStatsFor(bed);

        public override float GetWidth(float maxWidth)
        {
            return Mathf.Min(200f, maxWidth);
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
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
            var title = bed.Stats.title;
            Widgets.Label(rectTitle, title.Truncate(rectTitle.width));
            float y = (float) (rectInner.y + (double) Text.LineHeight + Text.SpaceBetweenLines + 7.0);
            GUI.color = GuestBedStatsColor;
            Text.Font = GameFont.Tiny;
            IEnumerable<TaggedString> stats = bed.Stats.textAsArray;
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

            // Royal title info box
            if (!Mouse.IsOver(rectWindow) || !ModLister.RoyaltyInstalled)
                return new GizmoResult(GizmoState.Clear);
            Rect windowRect = BedStatsDrawer.GetWindowRect();
            Find.WindowStack.ImmediateWindow(74975, windowRect, WindowLayer.Super, () => {
                // This is only going to have one instance, even if multiple beds are selected, thus it will only calculate
                // the stats for one bed at a given time, and this is cached to only update once per second
                bed.UpdateRoyaltyStats();
                BedStatsDrawer.DoBedInfos(windowRect, bed);
            });

            // Can't get this to work :(
            //if (Widgets.ButtonInvisible(rectWindow))
            //{
            //    ProcessInput(Event.current);
            //    return new GizmoResult(GizmoState.Interacted, Event.current);
            //}

            //if (Mouse.IsOver(rectWindow))
            //{
            //    TipSignal tip = new TipSignal(() => "ClickToLearnMore".Translate().Resolve(), (int) y * 37);
            //    TooltipHandler.TipRegion(rectWindow, tip);
            //}
            return new GizmoResult(GizmoState.Mouseover);
        }

        // Doesn't work. Never gets called :(
        public override void ProcessInput(Event ev)
        {
            Faction possibleFaction = Find.FactionManager.AllFactionsListForReading.First(f=>f.def.HasRoyalTitles);
            RoyalTitleDef titleDef = possibleFaction.def.RoyalTitlesAllInSeniorityOrderForReading.First();
            Log.Message($"{possibleFaction.GetCallLabel()} - {titleDef.LabelCap}");
            Find.WindowStack.Add(new Dialog_InfoCard(titleDef, possibleFaction));
        }
    }
}
