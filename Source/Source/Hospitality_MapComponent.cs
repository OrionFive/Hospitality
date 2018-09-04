using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Hospitality
{
    public class Hospitality_MapComponent : MapComponent
    {
        public static Hospitality_MapComponent Instance(Map map)
        {
            {
                return map.GetComponent<Hospitality_MapComponent>() ?? new Hospitality_MapComponent(true, map);
            }
        }

        private IncidentQueue incidentQueue = new IncidentQueue();
        private Dictionary<int, int> bribeCount = new Dictionary<int, int>(); // uses faction.randomKey
        public PrisonerInteractionModeDef defaultInteractionMode;
        public Area defaultAreaRestriction;
        public Area defaultAreaShopping;
        private int lastEventKey;

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref bribeCount, "bribeCount", LookMode.Value, LookMode.Value);
            Scribe_Defs.Look(ref defaultInteractionMode, "defaultInteractionMode");
            Scribe_References.Look(ref defaultAreaRestriction, "defaultAreaRestriction");
            Scribe_References.Look(ref defaultAreaShopping, "defaultAreaShopping");
            Scribe_Values.Look(ref lastEventKey, "lastEventKey");
            Scribe_Deep.Look(ref incidentQueue, "incidentQueue");

            if (defaultAreaRestriction == null) defaultAreaRestriction = map.areaManager.Home;
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
            defaultInteractionMode = PrisonerInteractionModeDefOf.NoInteraction;
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (incidentQueue == null) incidentQueue = new IncidentQueue();
            if(incidentQueue.Count == 0) FillIncidentQueue();
            incidentQueue.IncidentQueueTick();
        }

        private void FillIncidentQueue()
        {
            // Add some visits
            float days = Rand.Range(8f, 15f);
            int amount = Rand.Range(2, 5);
            foreach (var faction in Find.FactionManager.AllFactionsVisible.Where(f => !f.IsPlayer && f != Faction.OfPlayer && !f.HostileTo(Faction.OfPlayer)).OrderByDescending(f => f.PlayerGoodwill))
            {
                amount--;
                Log.Message(faction.GetCallLabel() + " are coming after " + days + " days.");
                GuestUtility.PlanNewVisit(map, days, faction);
                days += Rand.Range(10f, 15f);
                if (amount <= 0) break;
            }
        }

        public void QueueIncident(FiringIncident incident, float afterDays)
        {
            var qi = new QueuedIncident(incident, (int)(Find.TickManager.TicksGame + GenDate.TicksPerDay * afterDays));
            incidentQueue.Add(qi);
            //Log.Message("Queued Hospitality incident after " + afterDays + " days. Queue has now " + incidentQueue.Count + " items.");
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
    }
}
