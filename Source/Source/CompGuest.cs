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

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.LookValue(ref rescued, "rescued");
            Scribe_Values.LookValue(ref chat, "chat");
            Scribe_Values.LookValue(ref recruit, "recruit");
            Scribe_Collections.LookList(ref boughtItems, "boughtItems", LookMode.Value);
            if(boughtItems == null) boughtItems = new List<int>();
        }

        public void OnRescued()
        {
            rescued = true;
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            var pawn = parent as Pawn;
            if (pawn == null || !pawn.Spawned || pawn.Dead) return;

            if (rescued) RescuedCheck();

            //Log.Message((boughtItems == null) + " boughtItems of " + pawn.NameStringShort);
            //if (boughtItems.Count > 0)
            //{
            //    Log.Message(pawn.NameStringShort + ": " + GenText.ToCommaList(boughtItems.Select(i => i.Label)));
            //}
        }

        private void RescuedCheck()
        {
            var pawn = (Pawn) parent;
            if (pawn.Faction == Faction.OfPlayer)
            {
                rescued = false;
                return;
            }
            if (pawn.Downed || pawn.InBed()) return;

            // Can walk again, make the roll
            rescued = false;

            // Copied from Pawn_GuestTracker
            if (pawn.RaceProps.Humanlike && pawn.HostFaction == Faction.OfPlayer && !pawn.IsPrisoner)
            {
                if (!GuestUtility.WillRescueJoin(pawn)) return;

                GuestUtility.ShowRescuedPawnDialog(pawn);
            }
        }
    }
}