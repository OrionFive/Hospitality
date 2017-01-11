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
        private Area guestArea_int;
        public Area GuestArea
        {
            get
            {
                if (Pawn.playerSettings != null) return Pawn.playerSettings.AreaRestrictionInPawnCurrentMap;
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
            Scribe_Values.LookValue(ref rescued, "rescued");
            Scribe_Values.LookValue(ref chat, "chat");
            Scribe_Values.LookValue(ref recruit, "recruit");
            Scribe_Collections.LookList(ref boughtItems, "boughtItems", LookMode.Value);
            Scribe_References.LookReference(ref guestArea_int, "guestArea");
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
    }
}