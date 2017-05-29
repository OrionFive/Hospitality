using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Hospitality
{
    public class CompGuest : ThingComp
    {
        public List<int> boughtItems = new List<int>();
        private bool rescued;
        public bool chat;
        public bool recruit;

        public bool arrived;
        public bool sentAway;
        public bool mayBuy;

        public readonly Dictionary<Pawn, int> failedCharms = new Dictionary<Pawn, int>();

        private Area guestArea_int;
        private DrugPolicy drugPolicy;

        public Area GuestArea
        {
            get
            {
                if (Pawn.playerSettings != null) return Pawn.playerSettings.EffectiveAreaRestrictionInPawnCurrentMap;
                return guestArea_int;
            }
            set
            {
                if (Pawn.playerSettings != null) Pawn.playerSettings.AreaRestriction = value;
                guestArea_int = value;
            }
        }

        private Pawn Pawn { get { return (Pawn) parent; } }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref rescued, "rescued");
            Scribe_Values.Look(ref arrived, "arrived");
            Scribe_Values.Look(ref mayBuy, "mayBuy");
            Scribe_Values.Look(ref chat, "chat");
            Scribe_Values.Look(ref recruit, "recruit");
            Scribe_Collections.Look(ref boughtItems, "boughtItems", LookMode.Value);
            Scribe_References.Look(ref guestArea_int, "guestArea");
            Scribe_Deep.Look(ref drugPolicy, "drugPolicy");
            if (boughtItems == null) boughtItems = new List<int>();
        }

        public void OnRescued()
        {
            rescued = true;
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (Pawn == null || !Pawn.Spawned || Pawn.Dead) return;

            if (rescued) RescuedCheck();

            //Log.Message((boughtItems == null) + " boughtItems of " + pawn.NameStringShort);
            //if (boughtItems.Count > 0)
            //{
            //    Log.Message(pawn.NameStringShort + ": " + GenText.ToCommaList(boughtItems.Select(i => i.Label)));
            //}
        }

        private void RescuedCheck()
        {
            if (Pawn.Faction == Faction.OfPlayer)
            {
                rescued = false;
                return;
            }
            if (Pawn.Downed || Pawn.InBed()) return;

            // Can walk again, make the roll
            rescued = false;

            // Copied from Pawn_GuestTracker
            if (Pawn.RaceProps.Humanlike && Pawn.HostFaction == Faction.OfPlayer && !Pawn.IsPrisoner)
            {
                if (!GuestUtility.WillRescueJoin(Pawn)) return;

                GuestUtility.ShowRescuedPawnDialog(Pawn);
            }
        }

        public void Arrive()
        {
            Pawn.playerSettings.AreaRestriction = guestArea_int;
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