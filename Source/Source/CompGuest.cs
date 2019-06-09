using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Hospitality
{
    public class CompGuest : ThingComp
    {
        public List<int> boughtItems = new List<int>();

        public bool chat;
        public bool recruit;

        public bool arrived;
        public bool sentAway;

        public Lord lord;

        public readonly Dictionary<Pawn, int> failedCharms = new Dictionary<Pawn, int>();

        private Area guestArea_int;
        private Area shoppingArea_int;

        private DrugPolicy drugPolicy;

        public void ResetForGuest(Lord lord)
        {
            boughtItems.Clear();
            arrived = false;
            sentAway = false;
            failedCharms.Clear();
            this.lord = lord;
        }

        public Area GuestArea
        {
            get
            {
                if (guestArea_int != null && guestArea_int.Map != Pawn.MapHeld) return null;
                return guestArea_int;
            }
            set => guestArea_int = value;
        }

        public Area ShoppingArea
        {
            get
            {
                if (shoppingArea_int != null && shoppingArea_int.Map != Pawn.MapHeld) return null;
                return shoppingArea_int;
            }
            set => shoppingArea_int = value;
        }

        private Pawn Pawn => (Pawn) parent;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref arrived, "arrived");
            Scribe_Values.Look(ref chat, "chat");
            Scribe_Values.Look(ref recruit, "recruit");
            Scribe_Collections.Look(ref boughtItems, "boughtItems", LookMode.Value);
            Scribe_References.Look(ref guestArea_int, "guestArea");
            Scribe_References.Look(ref shoppingArea_int, "shoppingArea");
            Scribe_Deep.Look(ref drugPolicy, "drugPolicy");
            if (boughtItems == null) boughtItems = new List<int>();

            // Can't save lord (IExposable), so we just gotta find it each time
            if (Scribe.mode == LoadSaveMode.PostLoadInit) lord = Pawn.GetLord();
        }

        public void Arrive()
        {
            arrived = true;
        }

        public void Leave()
        {
            arrived = false;
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
    }
}