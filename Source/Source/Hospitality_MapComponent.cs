using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Hospitality
{
    public class Hospitality_MapComponent : MapComponent
    {
        private IncidentQueue incidentQueue = new IncidentQueue();
        public bool defaultEntertain;
        public bool defaultMakeFriends;
        public bool guestsAreWelcome = true;
        public Area defaultAreaRestriction;
        public Area defaultAreaShopping;
        public bool refuseGuestsUntilWeHaveBeds;
        private int nextQueueInspection;
        private int nextRogueGuestCheck;
        private int nextGuestListCheck;

        public List<Lord> PresentLords { get; } = new List<Lord>();
        public HashSet<Pawn> presentGuests = new HashSet<Pawn>();
        public IEnumerable<Pawn> PresentGuests => presentGuests;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref defaultEntertain, "defaultEntertain");
            Scribe_Values.Look(ref defaultMakeFriends, "defaultMakeFriends");
            Scribe_Values.Look(ref guestsAreWelcome, "guestsAreWelcome", true);
            Scribe_References.Look(ref defaultAreaRestriction, "defaultAreaRestriction");
            Scribe_References.Look(ref defaultAreaShopping, "defaultAreaShopping");
            Scribe_Deep.Look(ref incidentQueue, "incidentQueue");
            Scribe_Values.Look(ref refuseGuestsUntilWeHaveBeds, "refuseGuestsUntilWeHaveBeds");
            Scribe_Values.Look(ref nextQueueInspection, "nextQueueInspection");

            if (defaultAreaRestriction == null) defaultAreaRestriction = map.areaManager.Home;
        }

        public Hospitality_MapComponent(Map map) : base(map)
        {
            defaultAreaRestriction = map.areaManager.Home;
            
            RefreshGuestListTotal();
        }

        public override void FinalizeInit()
        {
            if (GuestCacher.CachedComponents.Length < Find.Maps.Count)
            {
                Array.Resize(ref GuestCacher.CachedComponents, Find.Maps.Count + 6); // This does Array.Copy for us.
            }   

            GuestCacher.CachedComponents[this.map.Index] = this;
        }

        public void RefreshGuestListTotal()
        {
            PresentLords.Clear();
            PresentLords.AddRange(map.lordManager.lords.Where(l => l.CurLordToil?.GetType() == typeof(LordToil_VisitPoint)));

            presentGuests = PresentLords.SelectMany(l => l.ownedPawns).ToHashSet();
        }

        public void OnLordArrived(Lord lord)
        {
            PresentLords.AddDistinct(lord);

            presentGuests = PresentLords.SelectMany(l => l.ownedPawns).ToHashSet();
        }

        public void OnLordLeft(Lord lord)
        {
            PresentLords.Remove(lord);

            presentGuests = PresentLords.SelectMany(l => l.ownedPawns).ToHashSet();
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (incidentQueue == null) incidentQueue = new IncidentQueue();
            if (incidentQueue.Count <= 1) GenericUtility.FillIncidentQueue(map);
            incidentQueue.IncidentQueueTick();

            if (GenTicks.TicksGame > nextQueueInspection)
            {
                nextQueueInspection = GenTicks.TicksGame + GenDate.TicksPerDay;
                GenericUtility.CheckTooManyIncidentsAtOnce(incidentQueue);
            }

            if (GenTicks.TicksGame > nextRogueGuestCheck)
            {
                nextRogueGuestCheck = GenTicks.TicksGame + GenDate.TicksPerHour;
                GuestUtility.CheckForRogueGuests(map);
            }

            if (GenTicks.TicksGame > nextGuestListCheck)
            {
                nextGuestListCheck = GenTicks.TicksGame + GenDate.TicksPerDay / 4;
                PresentLords.Clear();
                RefreshGuestListTotal();
            }
        }

        public void QueueIncident(FiringIncident incident, float afterDays)
        {
            var qi = new QueuedIncident(incident, (int)(Find.TickManager.TicksGame + GenDate.TicksPerDay * afterDays));
            incidentQueue.Add(qi);
            //Log.Message("Queued Hospitality incident after " + afterDays + " days. Queue has now " + incidentQueue.Count + " items.");
        }

        public QueuedIncident GetNextVisit(Faction faction)
        {
            QueuedIncident nearest = null;

            // Find earliest
            foreach (QueuedIncident incident in incidentQueue)
            {
                if (incident.FiringIncident.parms.faction == faction)
                {
                    if (nearest == null || incident.FireTick < nearest.FireTick) nearest = incident;
                }
            }
            return nearest;
        }

        public static void RefuseGuestsUntilWeHaveBeds(Map map)
        {
            if (map == null) return;

            var mapComp = map.GetMapComponent();
            mapComp.refuseGuestsUntilWeHaveBeds = true;
            LessonAutoActivator.TeachOpportunity(ConceptDef.Named("GuestBeds"), null, OpportunityType.Important);
        }
    }
}
