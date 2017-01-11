using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Hospitality
{
    public sealed class Event : IExposable
    {
        public int delayTicks;
        public List<EventAction> actions = new List<EventAction>();
        public int key;

        public void ExposeData()
        {
            Scribe_Values.LookValue(ref delayTicks, "delayTicks");
            Scribe_Collections.LookList(ref actions, "actions", LookMode.Deep);
        }
    }

    public class Hospitality_MapComponent : MapComponent
    {
        public static Hospitality_MapComponent Instance(Map map)
        {
            {
                return map.GetComponent<Hospitality_MapComponent>() ?? new Hospitality_MapComponent(true, map);
            }
        }

        private List<Event> eventQueue = new List<Event>();
        private Dictionary<int, int> bribeCount = new Dictionary<int, int>(); // uses faction.randomKey
        public PrisonerInteractionMode defaultInteractionMode;
        public Area defaultAreaRestriction;
        private int lastEventKey;

        public override void ExposeData()
        {
            Scribe_Collections.LookDictionary(ref bribeCount, "bribeCount", LookMode.Value, LookMode.Value);
            Scribe_Values.LookValue(ref defaultInteractionMode, "defaultInteractionMode", PrisonerInteractionMode.NoInteraction);
            Scribe_References.LookReference(ref defaultAreaRestriction, "defaultAreaRestriction");
            Scribe_Values.LookValue(ref lastEventKey, "lastEventKey", 0);
            Scribe_Collections.LookList(ref eventQueue, "eventQueue", LookMode.Deep);
        }

        public Hospitality_MapComponent(Map map) : base(map)
        {
        }

        public Hospitality_MapComponent(bool forReal, Map map) : base(map)
        {
            // Multi-Threading killed the elegant solution
            if (!forReal) return;
            map.components.Add(this);
            defaultAreaRestriction = map.areaManager.Home;
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (eventQueue == null) eventQueue = new List<Event>();
            var triggeredEvents = eventQueue.Where(e => --e.delayTicks <= 0).ToArray();

            foreach (var e in triggeredEvents)
            {
                eventQueue.Remove(e);

                foreach (var action in e.actions)
                {
                    action.DoAction();
                }
            }
        }

        public int GetBribeCount(Faction faction)
        {
            if (faction == null) throw new NullReferenceException("Faction not set.");
            int result;
            if (bribeCount.TryGetValue(faction.randomKey, out result)) return result;

            return 0;
        }

        public void Bribe(Faction faction)
        {
            if (faction == null) throw new NullReferenceException("Faction not set.");

            bribeCount[faction.randomKey] = GetBribeCount(faction) + 1;
        }

        public int QueueEvent(Event e)
        {
            e.key = ++lastEventKey;
            eventQueue.Add(e);
            return lastEventKey;
        }

        public Event GetEvent(int key)
        {
            return eventQueue.FirstOrDefault(e=>e.key == key);
        }
    }
}
