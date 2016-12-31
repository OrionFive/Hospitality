using RimWorld;
using Verse;

namespace Hospitality
{
    public class EventAction_AngerForPawn : EventAction
    {
        private Faction faction;
        private static readonly string labelMissingPawnsFactionAnger = "LetterLabelMissingPawnsFactionAnger".Translate();
        private static readonly string txtMessageKilledPawnFactionAnger = "MessageKilledPawnFactionAnger".Translate();
        private string pawnName;
        private float pawnValue;

        public EventAction_AngerForPawn(Pawn pawn, Faction faction)
        {
            pawnName = pawn.NameStringShort;
            pawnValue = 5; //pawn.RecruitPenalty(); dead! Very low value...
            this.faction = faction;
        }

        public EventAction_AngerForPawn()
        {
            //
        }

        public override void DoAction()
        {
            var penalty = pawnValue;
            if (penalty < 1) return;
            faction.AffectGoodwillWith(Faction.OfPlayer, -penalty);

            var message = string.Format(txtMessageKilledPawnFactionAnger, faction.Name, pawnName,
                (-penalty).ToStringByStyle(ToStringStyle.Integer, ToStringNumberSense.Offset));
            Find.LetterStack.ReceiveLetter(labelMissingPawnsFactionAnger, message, LetterType.BadNonUrgent);
        }

        public override void ExposeData()
        {
            Scribe_Values.LookValue(ref pawnName, "pawnName");
            Scribe_Values.LookValue(ref pawnValue, "pawnValue");
            Scribe_References.LookReference(ref faction, "faction");
        }
    }
}