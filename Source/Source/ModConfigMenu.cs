using System.Collections.Generic;
using System.Linq;
//using CommunityCoreLibrary;
using RimWorld;
using UnityEngine;
using Verse;

namespace Hospitality
{
    /*
    public class ModConfigMenu : ModConfigurationMenu
    {
        #region Fields

        public Dictionary<string, object> values = new Dictionary<string, object>();
        private float rowHeight = 24f;
        private float rowMargin = 6f;

        #endregion Fields

        #region Methods

        public override float DoWindowContents(Rect canvas)
        {
            float curY = 0f;

            // RELATIONS OPTIONS
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.LowerLeft;
            Widgets.Label(new Rect(0f, curY, canvas.width, rowHeight*2), "Fluffy_Relations.RelationOptions".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            curY += rowHeight*2;

            // min opinion threshold
            DrawLabeledInput(ref curY, canvas, "Lower opinion threshold", ref RelationsHelper.OPINION_THRESHOLD_NEG,
                "Pawns with an opinion of eachother below this threshold will always be visually linked.");

            // max opinion threshold
            DrawLabeledInput(ref curY, canvas, "Upper opinion threshold", ref RelationsHelper.OPINION_THRESHOLD_POS,
                "Pawns with an opinion of eachother above this threshold will always be visually linked.");

            foreach (var relation in DefDatabase<PawnRelationDef>.AllDefsListForReading)
            {
                Widgets.Label(new Rect(0f, curY, canvas.width/3f*2f, rowHeight), relation.LabelCap);
                bool active = RelationsHelper.RELATIONS_VISIBLE[relation];
                Widgets.Checkbox(new Vector2(canvas.width - (24f + rowMargin)*2f, curY), ref active);
                RelationsHelper.RELATIONS_VISIBLE[relation] = active;

                GUI.color = RelationsHelper.RELATIONS_COLOR[relation];
                GUI.DrawTexture(new Rect(canvas.width - 24f - rowMargin, curY, 24f, 24f), Resources.Solid);
                GUI.color = Color.white;
                curY += rowHeight + rowMargin;
            }

            // THOUGHT OPTIONS
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.LowerLeft;
            Widgets.Label(new Rect(0f, curY, canvas.width, rowHeight*2), "Fluffy_Relations.ThoughtOptions".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            curY += rowHeight*2;

            foreach (
                var thought in
                    DefDatabase<ThoughtDef>.AllDefsListForReading.Where(
                        t => RelationsHelper.THOUGHTS_SOCIAL[t] != Visible.inapplicable))
            {
                string label = thought.stages ? .
                First() ? .
                label? .
                CapitalizeFirst() ?? "<UNKNOWN>";
                Widgets.Label(new Rect(0f, curY, canvas.width/3f*2f, rowHeight), label);
                bool active = RelationsHelper.THOUGHTS_SOCIAL[thought] == Visible.visible;
                Widgets.Checkbox(new Vector2(canvas.width - (24f + rowMargin)*2f, curY), ref active);
                RelationsHelper.THOUGHTS_SOCIAL[thought] = active ? Visible.visible : Visible.hidden;
                curY += rowHeight + rowMargin;
            }

            // GRAPH OPTIONS
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.LowerLeft;
            Widgets.Label(new Rect(0f, curY, canvas.width, rowHeight*2), "Fluffy_Relations.GraphOptions".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            curY += rowHeight*2;

            string disclaimer = "Fluffy_Relations.GraphOptionsInformation".Translate();
            Text.Font = GameFont.Tiny;
            float disclaimerHeight = Text.CalcHeight(disclaimer, canvas.width);
            Widgets.Label(new Rect(0f, curY, canvas.width, disclaimerHeight), disclaimer);
            Text.Font = GameFont.Small;
            curY += disclaimerHeight;

            // max iterations
            DrawLabeledInput(ref curY, canvas, "Fluffy_Relations.Graph.MaxIterations".Translate(),
                ref Graph.MAX_ITERATIONS, "Fluffy_Relations.Graph.MaxIterationsTip".Translate());

            // movement threshold
            DrawLabeledInput(ref curY, canvas, "Fluffy_Relations.Graph.Threshold".Translate(), ref Graph.THRESHOLD,
                "Fluffy_Relations.Graph.ThresholdTip".Translate());

            // max temperature
            DrawLabeledInput(ref curY, canvas, "Fluffy_Relations.Graph.MaxTemperature".Translate(),
                ref Graph.MAX_TEMPERATURE, "Fluffy_Relations.Graph.MaxTemperatureTip".Translate());

            // centre gravitational force
            DrawLabeledInput(ref curY, canvas, "Fluffy_Relations.Graph.CentralConstant".Translate(),
                ref Graph.CENTRAL_CONSTANT, "Fluffy_Relations.Graph.CentralConstantTip".Translate());

            // attractive force
            DrawLabeledInput(ref curY, canvas, "Fluffy_Relations.Graph.AttractiveConstant".Translate(),
                ref Graph.ATTRACTIVE_CONSTANT, "Fluffy_Relations.Graph.AttractiveConstantTip".Translate());

            // repulsive force
            DrawLabeledInput(ref curY, canvas, "Fluffy_Relations.Graph.RepulsiveConstant".Translate(),
                ref Graph.REPULSIVE_CONSTANT, "Fluffy_Relations.Graph.RepulsiveConstantTip".Translate());

            return curY;
        }

        public void DrawLabeledInput(ref float curY, Rect canvas, string label, ref float value, string tip = "")
        {
            Widgets.Label(new Rect(0f, curY, canvas.width/3f*2f, rowHeight), label);

            if (!values.ContainsKey(label)) values.Add(label, value);

            GUI.SetNextControlName(label);
            values[label] = Widgets.TextField(new Rect(canvas.width/3f*2f, curY, canvas.width/3f, rowHeight),
                values[label].ToString());

            if (tip != "") TooltipHandler.TipRegion(new Rect(0f, curY, canvas.width, rowHeight), tip);

            if (GUI.GetNameOfFocusedControl() != label && !float.TryParse(values[label].ToString(), out value))
            {
                Messages.Message(
                    "Fluffy_Relations.InvalidFloat".Translate(
                        System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator),
                    MessageSound.RejectInput);
                values[label] = value;
            }

            curY += rowHeight + rowMargin;
        }

        public void DrawLabeledInput(ref float curY, Rect canvas, string label, ref int value, string tip = "")
        {
            Widgets.Label(new Rect(0f, curY, canvas.width/3f*2f, rowHeight), label);

            if (!values.ContainsKey(label)) values.Add(label, value);

            GUI.SetNextControlName(label);
            values[label] = Widgets.TextField(new Rect(canvas.width/3f*2f, curY, canvas.width/3f, rowHeight),
                values[label].ToString());

            if (tip != "") TooltipHandler.TipRegion(new Rect(0f, curY, canvas.width, rowHeight), tip);

            if (GUI.GetNameOfFocusedControl() != label && !int.TryParse(values[label].ToString(), out value))
            {
                Messages.Message("Fluffy_Relations.InvalidInteger".Translate(), MessageSound.RejectInput);
                values[label] = value;
            }

            curY += rowHeight + rowMargin;
        }

        public override void ExposeData()
        {
            // Graph parameters
            Scribe_Values.Look(ref Graph.ATTRACTIVE_CONSTANT, "ATTRACTIVE_CONSTANT");
            Scribe_Values.Look(ref Graph.CENTRAL_CONSTANT, "CENTRAL_CONSTANT");
            Scribe_Values.Look(ref Graph.MAX_ITERATIONS, "MAX_ITERATIONS");
            Scribe_Values.Look(ref Graph.MAX_TEMPERATURE, "MAX_TEMPERATURE");
            Scribe_Values.Look(ref Graph.REPULSIVE_CONSTANT, "REPULSIVE_CONSTANT");
            Scribe_Values.Look(ref Graph.THRESHOLD, "THRESHOLD");

            // Relation drawing parameters
            Scribe_Values.Look(ref RelationsHelper.OPINION_THRESHOLD_NEG, "OPINION_THRESHOLD_NEG");
            Scribe_Values.Look(ref RelationsHelper.OPINION_THRESHOLD_POS, "OPINION_THRESHOLD_POS");

            // DefMap with colours isn't scribed correctly.
            //Scribe_Deep.LookDeep( ref RelationsHelper.RELATIONS_COLOR, "RELATIONS_COLOR" );
            Scribe_Deep.LookDeep(ref RelationsHelper.RELATIONS_VISIBLE, "RELATIONS_VISIBLE");
            Scribe_Deep.LookDeep(ref RelationsHelper.THOUGHTS_SOCIAL, "THOUGHTS_SOCIAL");
        }

        #endregion Methods
    }*/
}