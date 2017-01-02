using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Hospitality
{
    public class IncidentWorker_WandererJoin : IncidentWorker
    {
        public override bool TryExecute(IncidentParms parms)
        {
            var map = (Map) parms.target;
            IntVec3 loc;
            if (!CellFinder.TryFindRandomEdgeCellWith(c => map.reachability.CanReachColony(c), map, out loc))
            {
                return false;
            }

            IntVec3 spawnSpot;
            if (!CellFinder.TryFindRandomEdgeCellWith(c => map.reachability.CanReachColony(c), map, out spawnSpot))
            {
                return false;
            }

            var pawn = GenericUtility.GetAnyRelatedWorldPawn(other => other.Faction != null && !other.Faction.HostileTo(Faction.OfPlayer), 100) ?? CreateNewPawn();
            if (pawn == null) return false;

            ShowWandererJoinDialog(pawn, spawnSpot, map);
            return true;
        }

        private static Pawn CreateNewPawn()
        {
            PawnKindDef pawnKindDef = new List<PawnKindDef> { PawnKindDefOf.Villager, PawnKindDefOf.Drifter, PawnKindDefOf.Slave }.RandomElement();

            // Get a non-player faction
            Faction otherFaction;
            if (Find.FactionManager.TryGetRandomNonColonyHumanlikeFaction(out otherFaction, true)) {} // Get a non medieval faction
            else if (Find.FactionManager.TryGetRandomNonColonyHumanlikeFaction(out otherFaction, false, true)) {} // No? Then medieval is ok, also defeated is ok
            else return null; // Nope, nothing. Everyone's dead?

            var request = new PawnGenerationRequest(pawnKindDef, otherFaction, PawnGenerationContext.NonPlayer, null, false, false, false, false, true, false, 20f, false, true, true, null, null, null, null, null, null);
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            return pawn;
        }

        public static void ShowWandererJoinDialog(Pawn pawn, IntVec3 spawnSpot, Map map)
        {
            // Added option to reject wanderer

            string textAsk = "WandererInitial".Translate(pawn.Faction.Name, pawn.story.adulthood.Title.ToLower(), GenText.ToCommaList(pawn.story.traits.allTraits.Select(t=>t.Label)));
            textAsk = textAsk.AdjustedFor(pawn);
            PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref textAsk, pawn);
            DiaNode nodeAsk = new DiaNode(textAsk);
            var textAccept = "RescuedInitial_Accept".Translate();
            textAccept = textAccept.AdjustedFor(pawn);

            DiaOption optionAccept = new DiaOption(textAccept);
            optionAccept.action = () => {
                if(Find.WorldPawns.Contains(pawn)) Find.WorldPawns.RemovePawn(pawn);
                GenSpawn.Spawn(pawn, spawnSpot, map);
                if (pawn.Faction != Faction.OfPlayer)
                {
                    if (pawn.Faction != null && pawn == pawn.Faction.leader)
                    {
                        pawn.Faction.GenerateNewLeader();
                    }
                    pawn.SetFaction(Faction.OfPlayer);
                }

                Find.CameraDriver.JumpTo(pawn.Position);
            };
            optionAccept.resolveTree = true;
            nodeAsk.options.Add(optionAccept);

            var textReject = "RescuedInitial_Reject".Translate();
            textReject = textReject.AdjustedFor(pawn);

            DiaOption optionReject = new DiaOption(textReject);
            optionReject.action = () => { GuestUtility.BreakupRelations(pawn); };
            optionReject.resolveTree = true;

            nodeAsk.options.Add(optionReject);
            Find.WindowStack.Add(new Dialog_NodeTree(nodeAsk, true));
        }
    }
}