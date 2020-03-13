using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Hospitality
{
    public class CompGuest : ThingComp
    {
        public List<int> boughtItems = new List<int>();

        public bool entertain;
        public bool makeFriends;

        public bool arrived;
        public bool sentAway;
        public bool rescued;

        public Lord lord;

        public readonly Dictionary<Pawn, int> failedCharms = new Dictionary<Pawn, int>();

        private Area guestArea_int;
        private Area shoppingArea_int;

        private DrugPolicy drugPolicy;

        public Building_GuestBed bed;
        public int lastBedCheckTick;

        public void ResetForGuest(Lord lord)
        {
            boughtItems.Clear();
            arrived = false;
            sentAway = false;
            failedCharms.Clear();
            this.lord = lord;
            Pawn.ownership.UnclaimBed();
        }

        private Pawn Pawn => (Pawn) parent;

        public bool HasBed => bed != null && bed.Spawned && bed.OwnersForReading.Contains(Pawn);
        

        public Area GuestArea
        {
            get
            {
                if (guestArea_int != null && guestArea_int.Map != Pawn.MapHeld) return null;
                if (!Pawn.MapHeld.areaManager.AllAreas.Contains(guestArea_int)) guestArea_int = null; // Area might be removed by player
                return guestArea_int;
            }
            set => guestArea_int = value;
        }

        public Area ShoppingArea
        {
            get
            {
                if (shoppingArea_int != null && shoppingArea_int.Map != Pawn.MapHeld) return null;
                if (!Pawn.MapHeld.areaManager.AllAreas.Contains(shoppingArea_int)) shoppingArea_int = null; // Area might be removed by player
                return shoppingArea_int;
            }
            set => shoppingArea_int = value;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref arrived, "arrived");
            Scribe_Values.Look(ref rescued, "rescued");
            Scribe_Values.Look(ref entertain, "chat");
            Scribe_Values.Look(ref makeFriends, "recruit");
            Scribe_Collections.Look(ref boughtItems, "boughtItems", LookMode.Value);
            Scribe_References.Look(ref guestArea_int, "guestArea");
            Scribe_References.Look(ref shoppingArea_int, "shoppingArea");
            Scribe_References.Look(ref bed, "bed");
            Scribe_Deep.Look(ref drugPolicy, "drugPolicy");
            if (boughtItems == null) boughtItems = new List<int>();

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                // Can't save lord (IExposable), so we just gotta find it each time
                lord = Pawn.GetLord();
                // Bed doesn't store owners
                if(bed != null && !bed.OwnersForReading.Contains(Pawn)) bed.CompAssignableToPawn.TryAssignPawn(Pawn);
            }
        }

        /// <summary>
        /// Only call from Pawn_Ownership_Patch!
        /// </summary>
        internal void ClearOwnership()
        {
            // Calling this method directly crashes the game (infinite loop, somehow). So here's a copy.
            Action<CompAssignableToPawn> TryUnassignPawn = comp => {
                var assignedPawns = Traverse.Create(comp).Field<List<Pawn>>("assignedPawns").Value;
                if (!assignedPawns.Contains(Pawn)) return;
                assignedPawns.Remove(Pawn);
                Traverse.Create(comp).Method("SortAssignedPawns").GetValue();
            };

            if (bed?.CompAssignableToPawn != null)
            {
                TryUnassignPawn.Invoke(bed?.CompAssignableToPawn);
            }
            bed = null;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            lord = Pawn.GetLord();
        }

        public void Arrive()
        {
            arrived = true;
        }

        public void Leave()
        {
            arrived = false;
            Pawn.ownership.UnclaimBed();
        }

        public DrugPolicy GetDrugPolicy(Pawn pawn)
        {
            if (drugPolicy == null)
            {
                drugPolicy = new DrugPolicy(pawn.thingIDNumber, "GuestDrugPolicy");
                drugPolicy.InitializeIfNeeded();
            }
            return drugPolicy;
        }

        public void ClaimBed(Building_GuestBed newBed)
        {
            if (!newBed.AnyUnownedSleepingSlot) return;

            var allOtherBeds = newBed.Map.listerBuildings.AllBuildingsColonistOfClass<Building_Bed>().Where(b => b != newBed);

            foreach (var otherBed in allOtherBeds)
            {
                if (otherBed.OwnersForReading.Contains(Pawn)) Log.Warning($"{Pawn.LabelShort} already owns {otherBed.Label}!");
            }

            Pawn.ownership.UnclaimBed();
            // @@@@Attention

            newBed.CompAssignableToPawn.TryAssignPawn(Pawn);
            bed = newBed;
            //Log.Message($"{Pawn.LabelShort} proudly claims {newBed.Label}!");
        }
    }
}