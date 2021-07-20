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
        private DrugPolicy drugPolicy;

        [NotNull]public List<Lord> PresentLords { get; } = new List<Lord>();
        [NotNull]public readonly HashSet<Pawn> presentGuests = new HashSet<Pawn>();
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
            Scribe_Deep.Look(ref drugPolicy, "drugPolicy");

            defaultAreaRestriction ??= map.areaManager.Home;
        }

        public Hospitality_MapComponent(Map map) : base(map)
        {
            defaultAreaRestriction = map.areaManager.Home;
        }

        public override void FinalizeInit()
        {
            ComponentCache.Register(this);
        }

        public void RefreshGuestListTotal()
        {
            PresentLords.Clear();
            // We look for the job of our lord to determine whether it is a guest group or not.
            PresentLords.AddRange(map.lordManager.lords.Where(l => l.LordJob is LordJob_VisitColony visit && !visit.leaving));
            //Log.Message($"Present lords: {PresentLords.Select(l => $"{l?.faction?.Name} ({l?.ownedPawns?.Count})").ToCommaList()}");
            MainTabWindowUtility.NotifyAllPawnTables_PawnsChanged();

            presentGuests.Clear();
            presentGuests.AddRange(PresentLords.SelectMany(l => l.ownedPawns));
        }

        public void OnLordArrived(Lord lord)
        {
            PresentLords.AddDistinct(lord);

            presentGuests.Clear();
            presentGuests.AddRange(PresentLords.SelectMany(l => l.ownedPawns));
        }

        public void OnLordLeft(Lord lord)
        {
            PresentLords.Remove(lord);

            presentGuests.Clear();
            presentGuests.AddRange(PresentLords.SelectMany(l => l.ownedPawns));
        }

        public void OnGuestAdopted(Pawn guest)
        {
            presentGuests.Remove(guest);
        }

        public void OnWorldLoaded()
        {
            RefreshGuestListTotal();
            CheckForCorrectDrugPolicies();
            ApplyCorrectFoodRestrictions();
        }

        private void ApplyCorrectFoodRestrictions()
        {
            foreach (var pawn in PresentGuests)
            {
                if (pawn.foodRestriction != null)
                {
                    pawn.foodRestriction.CurrentFoodRestriction = Current.Game.GetComponent<Hospitality_GameComponent>().defaultFoodRestriction;
                }
            }
        }

        public void CheckForCorrectDrugPolicies()
        {
            List<Pawn> changed = new List<Pawn>();
            foreach (var pawn in PresentGuests)
            {
                if (pawn.drugs?.CurrentPolicy != GetDrugPolicy())
                {
                    pawn.drugs.CurrentPolicy = pawn.Map.GetMapComponent().GetDrugPolicy();
                    changed.Add(pawn);
                }
            }

            if (changed.Any())
            {
                // TODO: Remove this message again eventually. It's only relevant for updating save games. 25/2/2021
                Log.Message($"Hospitality: Changed how DrugPolicies are stored. Fixed policies for {changed.Select(p => p.Name.ToStringShort).ToCommaList(true)}.");
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            incidentQueue ??= new IncidentQueue();
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

        public DrugPolicy GetDrugPolicy()
        {
            if (drugPolicy == null)
            {
                drugPolicy = new DrugPolicy(map.uniqueID, "GuestDrugPolicy");
                drugPolicy.InitializeIfNeeded();

                for (int i = 0; i < drugPolicy.Count; i++)
                {
                    var entry = drugPolicy[i];
                    var properties = entry.drug.GetCompProperties<CompProperties_Drug>();
                    if (entry.drug.IsPleasureDrug && properties?.addictiveness < 0.025f && !properties.CanCauseOverdose)
                    {
                        entry.allowedForJoy = true;
                        Log.Message($"Hospitality: Guests may use {entry.drug.label} for joy.");
                    }
                }
            }

            return drugPolicy;
        }

        public IEnumerable<QueuedIncident> GetQueuedVisits(float withinDays) => incidentQueue.queuedIncidents.Where(i => (i.FireTick - GenTicks.TicksGame + 0f) / GenDate.TicksPerDay < withinDays);
    }
}
