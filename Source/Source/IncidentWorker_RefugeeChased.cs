using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Hospitality
{
    public class IncidentWorker_RefugeeChased : IncidentWorker
    {
        private const float RaidPointsFactor = 1.35f;

        private static readonly IntRange RaidDelay = new IntRange(1000, 2500);

        public override bool TryExecute(IncidentParms parms)
        {
            IntVec3 spawnSpot;
            if (!CellFinder.TryFindRandomEdgeCellWith(c => c.CanReachColony(), out spawnSpot))
            {
                return false;
            }

            Pawn refugee = GenericUtility.GetAnyRelatedWorldPawn(other => other.Faction != null && other.Faction.HostileTo(Faction.OfPlayer) && HasGroupMakers(other.Faction), 100);
            if (refugee == null) return false;

            refugee.relations.everSeenByPlayer = true;
            Faction enemyFac = refugee.Faction;

            string text = "RefugeeChasedInitial".Translate(refugee.Name.ToStringFull, refugee.story.adulthood.title.ToLower(), enemyFac.def.pawnsPlural, enemyFac.Name, refugee.ageTracker.AgeBiologicalYears);
            text = text.AdjustedFor(refugee);
            PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref text, refugee);
            DiaNode diaNode = new DiaNode(text);
            DiaOption diaOption = new DiaOption("RefugeeChasedInitial_Accept".Translate());
            diaOption.action = delegate
            {
                if(refugee == enemyFac.leader) enemyFac.GenerateNewLeader();

                Find.WorldPawns.RemovePawn(refugee);
                var canDrop = enemyFac.def.techLevel >= TechLevel.Spacer;

                if (canDrop)
                {
                    spawnSpot = DropCellFinder.TradeDropSpot();
                    TradeUtility.SpawnDropPod(spawnSpot, refugee);
                }
                else
                {
                    GenSpawn.Spawn(refugee, spawnSpot);
                }

                refugee.SetFaction(Faction.OfPlayer);
                Find.CameraDriver.JumpTo(spawnSpot);
                IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(Find.Storyteller.def, IncidentCategory.ThreatBig);
                incidentParms.forced = true;
                incidentParms.faction = enemyFac;
                incidentParms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                incidentParms.raidArrivalMode = canDrop ? PawnsArriveMode.CenterDrop : PawnsArriveMode.EdgeWalkIn;
                incidentParms.spawnCenter = spawnSpot;
                incidentParms.points *= RaidPointsFactor;
                

                QueuedIncident qi = new QueuedIncident(new FiringIncident(IncidentDefOf.RaidEnemy, null, incidentParms), Find.TickManager.TicksGame + RaidDelay.RandomInRange);
                Find.Storyteller.incidentQueue.Add(qi);
            };
            diaOption.resolveTree = true;
            diaNode.options.Add(diaOption);
            string text2 = "RefugeeChasedRejected".Translate(refugee.NameStringShort);
            DiaNode diaNode2 = new DiaNode(text2);
            DiaOption diaOption2 = new DiaOption("OK".Translate());
            diaOption2.resolveTree = true;
            diaNode2.options.Add(diaOption2);
            DiaOption diaOption3 = new DiaOption("RefugeeChasedInitial_Reject".Translate());
            diaOption3.action = delegate
            {
                HealthUtility.GiveInjuriesToKill(refugee);
                Log.Message(refugee.Name + " dead? " + refugee.Dead);
                //Find.WorldPawns.PassToWorld(refugee,PawnDiscardDecideMode.Discard);
            };
            diaOption3.link = diaNode2;
            diaNode.options.Add(diaOption3);
            Find.WindowStack.Add(new Dialog_NodeTree(diaNode, true, true));
            return true;
        }

        private static bool HasGroupMakers(Faction faction)
        {
            if (faction.def.pawnGroupMakers == null) return false;
            return faction.def.pawnGroupMakers.Any();
        }
    }
}