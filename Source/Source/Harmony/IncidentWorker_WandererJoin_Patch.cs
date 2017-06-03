using System.Collections.Generic;
using System.Linq;
using Harmony;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
    public class IncidentWorker_WandererJoin_Patch
    {
        // So we can show a dialog
        // So we can get related pawns first
        [HarmonyPatch(typeof (IncidentWorker_WandererJoin), "TryExecute")]
        public class TryExecute
        {

            [HarmonyPrefix]
            public static bool Replacement(IncidentParms parms, ref bool __result)
            {
                var map = (Map) parms.target;
                IntVec3 loc;
                if (!CellFinder.TryFindRandomEdgeCellWith(c => map.reachability.CanReachColony(c), map, CellFinder.EdgeRoadChance_Neutral, out loc))
                {
                    __result = false;
                    return false;
                }

                bool getRelated = Rand.Value < 0.7f;
                var pawn = (getRelated ? GenericUtility.GetAnyRelatedWorldPawn(other => other.Faction != null && !other.Faction.HostileTo(Faction.OfPlayer), 100) : null) ?? CreateNewPawn();
                if (pawn == null)
                {
                    __result = false;
                    return false;
                }

                ShowWandererJoinDialog(pawn, loc, map);
                __result = true;
                return false;
            }

            private static Pawn CreateNewPawn()
            {
                PawnKindDef pawnKindDef = new List<PawnKindDef> {PawnKindDefOf.Villager, PawnKindDefOf.Drifter, PawnKindDefOf.Slave}.RandomElement();

                // Get a non-player faction
                Faction otherFaction;
                if (Find.FactionManager.TryGetRandomNonColonyHumanlikeFaction(out otherFaction, true))
                {
                } // Get a non medieval faction
                else if (Find.FactionManager.TryGetRandomNonColonyHumanlikeFaction(out otherFaction, false, true))
                {
                } // No? Then medieval is ok, also defeated is ok
                else return null; // Nope, nothing. Everyone's dead?

                var request = new PawnGenerationRequest(pawnKindDef, otherFaction, PawnGenerationContext.NonPlayer, -1, false, false, false, false, true, false, 20f);
                Pawn pawn = PawnGenerator.GeneratePawn(request);
                return pawn;
            }

            public static void ShowWandererJoinDialog(Pawn pawn, IntVec3 spawnSpot, Map map)
            {
                // Added option to reject wanderer

                string textAsk = "WandererInitial".Translate(pawn.Faction.Name, pawn.GetTitle().ToLower(), GenText.ToCommaList(pawn.story.traits.allTraits.Select(t => t.Label)));
                textAsk = textAsk.AdjustedFor(pawn);
                PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref textAsk, pawn);
                DiaNode nodeAsk = new DiaNode(textAsk);
                var textAccept = "RescuedInitial_Accept".Translate();
                textAccept = textAccept.AdjustedFor(pawn);

                DiaOption optionAccept = new DiaOption(textAccept);
                optionAccept.action = () =>
                {
                    if (Find.WorldPawns.Contains(pawn)) Find.WorldPawns.RemovePawn(pawn);
                    GenSpawn.Spawn(pawn, spawnSpot, map);
                    if (pawn.Faction != Faction.OfPlayer)
                    {
                        if (pawn.Faction != null && pawn == pawn.Faction.leader)
                        {
                            pawn.Faction.GenerateNewLeader();
                        }
                        pawn.SetFaction(Faction.OfPlayer);
                    }

                    CameraJumper.TryJump(pawn);
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
}