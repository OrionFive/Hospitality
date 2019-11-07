using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Hospitality.Spacer {
    public abstract class IncidentWorker_VisitorGroupSpacer : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map) parms.target;
            if (Settings.disableGuests || map.mapPawns.ColonistCount == 0) return false;

            return base.CanFireNowSub(parms);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = (Map) parms.target;
            if(map == null) throw new NullReferenceException("Map is null!");

            return SpawnGroup(map, 10);
        }

        private bool SpawnGroup(Map map, int amount)
        {
            var visitors = new List<Pawn>();
            try
            {
                var spot = DropCellFinder.RandomDropSpot(map);
                if (!spot.IsValid)
                {
                    throw new Exception("Visitors failed to find a valid spawn location.");
                }

                SpawnPawns(visitors, amount, map, spot);

                SpawnGroupUtility.CheckVisitorsValid(visitors);

                if (visitors == null || visitors.Count == 0) return false;
                
                GiveItems(visitors);

                var stayDuration = (int)(Rand.Range(1f, 2.4f) * GenDate.TicksPerDay);
                CreateLord(FactionDefOf.Ancients, spot, visitors, map, true, true, stayDuration, Rand.Value < 0.75f);
            }
            catch (Exception e)
            {
                Log.Error($"Hospitality: Something failed when setting up visitors:\n{e.Message}\n{e.StackTrace}");
                foreach (var visitor in visitors)
                {
                    if (visitor?.Spawned == true) visitor.DestroyOrPassToWorld();
                }
            }
            return true; // be gone, event
        }

        private void SpawnPawns(List<Pawn> spawned, int amount, Map map, IntVec3 spot)
        {
            // Create some new people
            // TODO: Use custom ship def class to specify PawnGenOptions
            var newPawns = GeneratePawns(amount, map.Tile, options).ToList();
            Log.Message($"Created {newPawns.Count()} new pawns.");

            newPawns.Shuffle();
            
            foreach (var pawn in newPawns)
            {
                try
                {
                    var visitor = SpawnGroupUtility.SpawnVisitor(spawned, pawn, map, spot);
                    if (visitor.needs?.joy != null) visitor.needs.joy.CurLevel = Rand.Range(0.2f, 0.4f);
                    if (visitor.needs?.outdoors != null) visitor.needs.outdoors.CurLevel = Rand.Range(0.1f, 0.5f);
                    spawned.Add(visitor);
                }
                catch (Exception e)
                {
                    Log.Error($"Hospitality: Failed to spawn pawn {pawn?.Label}:\n{e.Message}\n{e.StackTrace}");
                    if(pawn.Spawned) pawn.DestroyOrPassToWorld();
                }
            }
        }

        private static IEnumerable<Pawn> GeneratePawns(int amount, int mapTile, List<PawnGenOption> options)
        {
            for (int i = 0; i < amount; i++)
            {
                PawnGenerationRequest request = new PawnGenerationRequest(options.RandomElementByWeight(o=>o.selectionWeight).kind, Faction.OfAncients,
                    PawnGenerationContext.NonPlayer, mapTile, false, false, false, false, false, 
                    false, 0, true, true, true, false, true);
                yield return PawnGenerator.GeneratePawn(request);
            }
            // TODO: Generate relations among guests?
        }
    }
}
