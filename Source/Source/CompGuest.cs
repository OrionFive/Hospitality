using System.Collections.Generic;
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
        }
    }
}