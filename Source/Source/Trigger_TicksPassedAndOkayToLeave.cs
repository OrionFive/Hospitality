using Verse.AI.Group;

namespace Hospitality
{
    public class Trigger_TicksPassedAndOkayToLeave : Trigger_TicksPassed
    {
        public Trigger_TicksPassedAndOkayToLeave(int tickLimit) : base(tickLimit) {}

        public override bool ActivateOn(Lord lord, TriggerSignal signal)
        {
            bool leave = base.ActivateOn(lord, signal);

            if (GuestUtility.GuestsShouldStayLonger(lord)) return false;
            return leave;
        }
    }
}