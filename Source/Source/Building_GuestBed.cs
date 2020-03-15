using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Hospitality
{
    public class Building_GuestBed : Building_Bed
    {
        private static readonly Color sheetColorForGuests = new Color(89 / 255f, 55 / 255f, 121 / 255f);

        private int feeStep = 10;

        public int rentalFee;
        private string silverLabel = " " + ThingDefOf.Silver.label;

        public string FeeString => rentalFee == 0 ? "FeeNone".Translate() : "FeeAmount".Translate(rentalFee);
        public string AttractivenessString => "BedAttractiveness".Translate(BedUtility.StaticBedValue(this, out _, out _, out _, out _)-rentalFee);
   
        public int MoodEffect => Mathf.RoundToInt(rentalFee * -0.1f);


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref rentalFee, "rentalFee");
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
            // Creating copy for iteration, since list changes during loop
            foreach (var owner in OwnersForReading.ToArray())
            {
                owner.ownership.UnclaimBed();
            }
            var room = this.GetRoom();
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
            stringBuilder.Append(AttractivenessString);

            stringBuilder.AppendLine();
            stringBuilder.Append(FeeString);
            
            stringBuilder.AppendLine();
            if (!OwnersForReading.Any())
            {
                stringBuilder.Append($"{"Owner".Translate()}: {"Nobody".Translate()}");
            }
            else if (OwnersForReading.Count == 1)
            {
                stringBuilder.Append($"{"Owner".Translate()}: {OwnersForReading[0].LabelShortCap}");
            }
            else
            {
                stringBuilder.Append("Owners".Translate() + ": ");
                bool notFirst = false;
                foreach (var owner in OwnersForReading)
                {
                    if (notFirst)
                    {
                        stringBuilder.Append(", ");
                    }
                    notFirst = true;
                    stringBuilder.Append(owner.LabelShortCap);
                }
                //if(notFirst) stringBuilder.AppendLine();
            }
            return stringBuilder.ToString();
        }

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

                if (!OwnersForReading.Any())
                {
                    GenMapUI.DrawThingLabel(this, rentalFee + silverLabel, defaultThingLabelColor);
                }
                else if (OwnersForReading.Count == 1)
                {
                    if (OwnersForReading[0].InBed() && OwnersForReading[0].CurrentBed() == this) return;
                    GenMapUI.DrawThingLabel(this, OwnersForReading[0].LabelShort, defaultThingLabelColor);
                }
                else
                {
                    for (int index = 0; index < OwnersForReading.Count; ++index)
                    {
                        if (!OwnersForReading[index].InBed() || OwnersForReading[index].CurrentBed() != this || !(OwnersForReading[index].Position == GetSleepingSlotPos(index)))
                        {
                            var pos = Traverse.Create(this).Method("GetMultiOwnersLabelScreenPosFor", index).GetValue<Vector3>();
                            GenMapUI.DrawThingLabel(pos, OwnersForReading[index].LabelShort, defaultThingLabelColor);
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
