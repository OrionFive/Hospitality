using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Hospitality
{
    public class Building_GuestBed : Building_Bed
    {
        private static readonly Color guestFieldColor = new Color(170/255f, 79/255f, 255/255f);

        private static readonly Color sheetColorForGuests = new Color(89/255f, 55/255f, 121/255f);

        private static readonly List<IntVec3> guestField = new List<IntVec3>();

        public Pawn CurOccupant
        {
            get
            {
                var list = Map.thingGrid.ThingsListAt(Position);
                return list.OfType<Pawn>()
                    .Where(pawn => pawn.jobs.curJob != null)
                    .FirstOrDefault(pawn => pawn.jobs.curJob.def == JobDefOf.LayDown && pawn.jobs.curJob.targetA.Thing == this);
            }
        }

        public override Color DrawColor
        {
            get
            {
                if (def.MadeFromStuff)
                {
                    return base.DrawColor;
                }
                return DrawColorTwo;
            }
        }

        public override void Draw()
        {
            base.Draw();
            if (Medical) Medical = false;
            if (ForPrisoners) ForPrisoners = false;
        }

        public override Color DrawColorTwo { get { return sheetColorForGuests; } }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            foreach (var owner in owners.ToArray())
            {
                owner.ownership.UnclaimBed();
            }
            var room = Position.GetRoom(Map);
            base.DeSpawn(mode);
            if (room != null)
            {
                room.Notify_RoomShapeOrContainedBedsChanged();
            }
        }

        //public override void DrawExtraSelectionOverlays()
        //{
        //    base.DrawExtraSelectionOverlays();
        //    var room = this.GetRoom();
        //    if (room == null) return;
        //    if (room.isPrisonCell) return;
        //
        //    if (room.RegionCount < 20 && !room.TouchesMapEdge)
        //    {
        //        foreach (var current in room.Cells)
        //        {
        //            guestField.Add(current);
        //        }
        //        var color = guestFieldColor;
        //        color.a = Pulser.PulseBrightness(1f, 0.6f);
        //        GenDraw.DrawFieldEdges(guestField, color);
        //        guestField.Clear();
        //    }
        //}

        public override string GetInspectString()
        {
            var stringBuilder = new StringBuilder();
            //stringBuilder.Append(base.GetInspectString());
            stringBuilder.Append(InspectStringPartsFromComps());
            stringBuilder.AppendLine();
            stringBuilder.Append("ForGuestUse".Translate());
            
            stringBuilder.AppendLine();
            if (owners.Count == 0)
            {
                stringBuilder.Append("Owner".Translate() + ": " + "Nobody".Translate());
            }
            else if (owners.Count == 1)
            {
                stringBuilder.Append("Owner".Translate() + ": " + owners[0].LabelCap);
            }
            else
            {
                stringBuilder.Append("Owners".Translate() + ": ");
                bool notFirst = false;
                foreach (Pawn owner in owners)
                {
                    if (notFirst)
                    {
                        stringBuilder.Append(", ");
                    }
                    notFirst = true;
                    stringBuilder.Append(owner.Label);
                }
                //if(notFirst) stringBuilder.AppendLine();
            }
            return stringBuilder.ToString();
        }

        // Is this one even being used??
        public override IEnumerable<Gizmo> GetGizmos()
        {
            // Get original gizmos from Building class
            var method = typeof(Building).GetMethod("GetGizmos");
            var ftn = method.MethodHandle.GetFunctionPointer();
            var func = (Func<IEnumerable<Gizmo>>)Activator.CreateInstance(typeof(Func<IEnumerable<Gizmo>>), this, ftn);

            foreach (var gizmo in func())
            {
                yield return gizmo;
            }

            if(def.building.bed_humanlike)
            {
                yield return
                        new Command_Toggle
                        {
                            defaultLabel = "CommandBedSetAsGuestLabel".Translate(),
                            defaultDesc = "CommandBedSetAsGuestDesc".Translate(),
                            icon = ContentFinder<Texture2D>.Get("UI/Commands/AsGuest"),
                            isActive = () => true,
                            toggleAction = () => Swap(this),
                            hotKey = KeyBindingDefOf.Misc4
                        };
            }
        }

        public override void PostMake()
        {
            base.PostMake();
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDef.Named("GuestBeds"), KnowledgeAmount.Total);
        }

        public override void DrawGUIOverlay()
        {
            //if (Find.CameraMap.CurrentZoom == CameraZoomRange.Closest)
            //{
            //    if (owner != null && owner.InBed() && owner.CurrentBed().owner == owner)
            //    {
            //        return;
            //    }
            //    string text;
            //    if (owner != null)
            //    {
            //        text = owner.NameStringShort;
            //    }
            //    else
            //    {
            //        text = "Unowned".Translate();
            //    }
            //    GenWorldUI.DrawThingLabel(this, text, new Color(1f, 1f, 1f, 0.75f));
            //}
        }

        public static void Swap(Building_Bed bed)
        {
            Building_Bed newBed;
            if (bed is Building_GuestBed)
            {
                newBed = (Building_Bed) MakeBed(bed, bed.def.defName.Split(new[] {"Guest"}, StringSplitOptions.RemoveEmptyEntries)[0]);
            }
            else
            {
                newBed = (Building_GuestBed) MakeBed(bed, bed.def.defName+"Guest");
            }
            newBed.SetFactionDirect(bed.Faction);
            var spawnedBed = (Building_Bed)GenSpawn.Spawn(newBed, bed.Position, bed.Map, bed.Rotation);
            spawnedBed.HitPoints = bed.HitPoints;
            spawnedBed.ForPrisoners = bed.ForPrisoners;

            var compQuality = spawnedBed.TryGetComp<CompQuality>();

            if(compQuality != null) compQuality.SetQuality(bed.GetComp<CompQuality>().Quality, ArtGenerationContext.Outsider);
            //var compArt = bed.TryGetComp<CompArt>();
            //if (compArt != null)
            //{
            //    var art = spawnedBed.GetComp<CompArt>();
            //    Traverse.Create(art).Field("authorNameInt").SetValue(Traverse.Create(compArt).Field("authorNameInt").GetValue());
            //    Traverse.Create(art).Field("titleInt").SetValue(Traverse.Create(compArt).Field("titleInt").GetValue());
            //    Traverse.Create(art).Field("taleRef").SetValue(Traverse.Create(compArt).Field("taleRef").GetValue());
            //
            //    // TODO: Make this work, art is now destroyed
            //}
            Find.Selector.Select(spawnedBed, false);
        }

        private static Thing MakeBed(Building_Bed bed, string defName)
        {
            ThingDef newDef = DefDatabase<ThingDef>.GetNamed(defName);
            return ThingMaker.MakeThing(newDef, bed.Stuff);
        }
    }
}
