using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
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

        public int rentalFee;
        private int feeStep = 10;
        private string silverLabel = " " + ThingDefOf.Silver.label;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref rentalFee, "rentalFee");
        }

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

        public override Color DrawColorTwo => sheetColorForGuests;

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            foreach (var owner in owners.ToArray())
            {
                owner.ownership.UnclaimBed();
            }
            var room = Position.GetRoom(Map);
            base.DeSpawn(mode);
            room?.Notify_RoomShapeOrContainedBedsChanged();
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
            stringBuilder.Append(FeeString);
            
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

        public string FeeString => rentalFee == 0 ? "FeeNone".Translate() : "FeeAmount".Translate(rentalFee);
        public int MoodEffect => Mathf.RoundToInt(rentalFee * -0.2f);

        // Note to whoever wants to add to this method (hi jptrrs!):
        // You can just do
        // foreach (Gizmo c in base.GetGizmos())
        // {
        //    yield return c;
        // }
        // yield return [whatever you want to add];
        //
        // No need to copy this method.
        public override IEnumerable<Gizmo> GetGizmos()
        {
            // Display the original gizmos (includes the swap guest bed button via patch)
            foreach (var gizmo in base.GetGizmos())
            {
                switch (gizmo)
                {
                    case Command_Toggle toggle: {
                        // Disable prisoner and medical buttons
                        if (toggle.defaultLabel == "CommandBedSetForPrisonersLabel".Translate() 
                            || toggle.defaultLabel == "CommandBedSetAsMedicalLabel".Translate()) gizmo.Disable();
                        break;
                    }
                    case Command_Action action: {
                        // Disable set owner button
                        if (action.defaultLabel == "CommandBedSetOwnerLabel".Translate()) action.Disable();
                        break;
                    }
                }
                yield return gizmo;
            }

            // Add buttons to decrease / increase the fee
            yield return new Command_Action
            {
                defaultLabel = "CommandBedDecreaseFeeLabel".Translate(feeStep),
                defaultDesc = "CommandBedDecreaseFeeDesc".Translate(feeStep, MoodEffect),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/ChangePriceDown"),
                action = () => AdjustFee(-feeStep),
                hotKey = KeyBindingDefOf.Misc5,
                disabled = rentalFee < feeStep
            };
            yield return new Command_Action
            {
                defaultLabel = "CommandBedIncreaseFeeLabel".Translate(feeStep),
                defaultDesc = "CommandBedIncreaseFeeDesc".Translate(feeStep, MoodEffect),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/ChangePriceUp"),
                action = () => AdjustFee(feeStep),
                hotKey = KeyBindingDefOf.Misc6
            };

            // Get base def
            var defName = def.defName.ReplaceFirst("Guest", string.Empty);
            var baseDef = DefDatabase<ThingDef>.GetNamed(defName);

            // Add build copy command
            Command buildCopy = BuildCopyCommandUtility.BuildCopyCommand(baseDef, Stuff);
            if (buildCopy != null) yield return buildCopy;
        }

        private void AdjustFee(int amount)
        {
            rentalFee += amount;
            if (rentalFee < 0) rentalFee = 0;
        }

        public override void PostMake()
        {
            base.PostMake();
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDef.Named("GuestBeds"), KnowledgeAmount.Total);
        }

        public override void DrawGUIOverlay()
        {
            if (Find.CameraDriver.CurrentZoom == CameraZoomRange.Closest)
            {
                Color defaultThingLabelColor = GenMapUI.DefaultThingLabelColor;

                if (!owners.Any())
                {
                    GenMapUI.DrawThingLabel(this, rentalFee + silverLabel, defaultThingLabelColor);
                }
                else if (owners.Count == 1)
                {
                    if (owners[0].InBed() && owners[0].CurrentBed() == this) return;
                    GenMapUI.DrawThingLabel(this, owners[0].LabelShort, defaultThingLabelColor);
                }
                else
                {
                    for (int index = 0; index < owners.Count; ++index)
                    {
                        if (!owners[index].InBed() || owners[index].CurrentBed() != this || !(owners[index].Position == GetSleepingSlotPos(index)))
                        {
                            var pos = Traverse.Create(this).Method("GetMultiOwnersLabelScreenPosFor", index).GetValue<Vector2>();
                            GenMapUI.DrawThingLabel(pos, owners[index].LabelShort, defaultThingLabelColor);
                        }
                    }
                }
            }
        }

        public static void Swap(Building_Bed bed)
        {
            Building_Bed newBed;
            if (IsGuestBed(bed))
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

            compQuality?.SetQuality(bed.GetComp<CompQuality>().Quality, ArtGenerationContext.Outsider);
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

        public static bool IsGuestBed(Building_Bed bed)
        {
            return bed is Building_GuestBed;
        }
    }
}
