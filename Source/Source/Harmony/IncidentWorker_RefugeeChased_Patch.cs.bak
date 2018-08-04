using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
    public class IncidentWorker_RefugeeChased_Patch
    {
        // So we can kill the rejected pawn
        // So we can get related pawns first
        [HarmonyPatch(typeof (IncidentWorker_RefugeeChased), "TryExecuteWorker")]
        public class TryExecuteWorker
        {

            private const float RaidPointsFactor = 1.35f;

            private static readonly IntRange RaidDelay = new IntRange(1000, 2500);

            #region Added

            private static readonly Func<Pawn, bool> _isLeader = other => other.Faction.leader == other && Rand.Value > 0.05f;
            private static readonly Func<Pawn, bool> _selector = other => other.RaceProps.Humanlike && other.Faction != null && other.Faction.HostileTo(Faction.OfPlayer) && HasGroupMakers(other.Faction) && !_isLeader(other);

            #endregion

            [HarmonyPrefix]
            public static bool Replacement(IncidentParms parms, ref bool __result)
            {
                Map map = (Map) parms.target;
                IntVec3 spawnSpot;
                if (!CellFinder.TryFindRandomEdgeCellWith(c => map.reachability.CanReachColony(c), map, CellFinder.EdgeRoadChance_Neutral, out spawnSpot))
                {
                    __result = false;
                    return false;
                }

                #region CHANGED

                Pawn refugee = Rand.Value < 0.7f ? GenericUtility.GetAnyRelatedWorldPawn(_selector, 100) : null;
                if (refugee == null)
                {
                    // Just ANYONE
                    Find.WorldPawns.AllPawnsAlive.Where(_selector).TryRandomElement(out refugee);
                }
                if (refugee == null)
                {
                    __result = false;
                    return false;
                }

                #endregion

                refugee.relations.everSeenByPlayer = true;
                Faction enemyFac;
                if (!(from f in Find.FactionManager.AllFactions
                    where !f.def.hidden && f.HostileTo(Faction.OfPlayer)
                    select f).TryRandomElement(out enemyFac))
                {
                    __result = false;
                    return false;
                }
                string text = "RefugeeChasedInitial".Translate(new object[]
                {
                    refugee.Name.ToStringFull,
                    refugee.story.Title.ToLower(),
                    enemyFac.def.pawnsPlural,
                    enemyFac.Name,
                    refugee.ageTracker.AgeBiologicalYears
                });
                text = text.AdjustedFor(refugee);
                PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref text, refugee);
                DiaNode diaNode = new DiaNode(text);
                DiaOption diaOption = new DiaOption("RefugeeChasedInitial_Accept".Translate());
                diaOption.action = delegate
                {
                    GenSpawn.Spawn(refugee, spawnSpot, map);
                    refugee.SetFaction(Faction.OfPlayer, null);
                    CameraJumper.TryJump(refugee);
                    IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
                    incidentParms.forced = true;
                    incidentParms.faction = enemyFac;
                    incidentParms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                    incidentParms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                    incidentParms.spawnCenter = spawnSpot;
                    incidentParms.points *= RaidPointsFactor;
                    QueuedIncident qi = new QueuedIncident(new FiringIncident(IncidentDefOf.RaidEnemy, null, incidentParms), Find.TickManager.TicksGame + RaidDelay.RandomInRange);
                    Find.Storyteller.incidentQueue.Add(qi);
                };
                diaOption.resolveTree = true;
                diaNode.options.Add(diaOption);
                string text2 = "RefugeeChasedRejected".Translate(new object[]
                {
                    refugee.Name.ToStringShort
                });
                DiaNode diaNode2 = new DiaNode(text2);
                DiaOption diaOption2 = new DiaOption("OK".Translate());
                diaOption2.resolveTree = true;
                diaNode2.options.Add(diaOption2);
                DiaOption diaOption3 = new DiaOption("RefugeeChasedInitial_Reject".Translate());
                diaOption3.action = delegate
                {
                    #region CHANGED

                    HealthUtility.DamageUntilDead(refugee);
                    //Log.Message(refugee.Name + " dead? " + refugee.Dead);
                    //Find.WorldPawns.PassToWorld(refugee,PawnDiscardDecideMode.Discard);

                    #endregion
                };
                diaOption3.link = diaNode2;
                diaNode.options.Add(diaOption3);
                string title = "RefugeeChasedTitle".Translate(new object[]
                {
                    map.info.parent.Label
                });
                Find.WindowStack.Add(new Dialog_NodeTree(diaNode, true, true, title));
                __result = true;
                return false;
            }

            private static bool HasGroupMakers(Faction faction)
            {
                if (faction.def.pawnGroupMakers == null) return false;
                return faction.def.pawnGroupMakers.Any();
            }
        }
    }
}