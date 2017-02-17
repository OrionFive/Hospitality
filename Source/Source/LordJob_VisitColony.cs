using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Hospitality
{
    public class LordJob_VisitColony : LordJob
    {
        private Faction faction;
        private IntVec3 chillSpot;
        private int stayDuration;
        private int checkEventId = -1;
        
        public LordJob_VisitColony()
        {
            // Required
        }

        public LordJob_VisitColony(Faction faction, IntVec3 chillSpot, int stayDuration)
        {
            this.faction = faction;
            this.chillSpot = chillSpot;
            this.stayDuration = stayDuration;
        }

        public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
        {
            base.Notify_PawnLost(p, condition);

            switch (condition)
            {
                case PawnLostCondition.ExitedMap:
                case PawnLostCondition.ChangedFaction:
                    //foreach (var action in e.actions.OfType<EventAction_Pawns>()) action.RemovePawn(p);
                    break;
                case PawnLostCondition.Undefined:
                case PawnLostCondition.Vanished:
                case PawnLostCondition.IncappedOrKilled:
                //case PawnLostCondition.MadePrisoner:
                //case PawnLostCondition.LeftVoluntarily:
                //case PawnLostCondition.Drafted:
                    Hospitality_MapComponent.Instance(lord.Map).QueueEvent(new Event
                    {
                        delayTicks = (int)(GenDate.TicksPerHour * Rand.Range(1f, 3f)),
                        actions =
                            new List<EventAction>
                    {
                        new EventAction_AngerForPawn(p, faction),
                        new EventAction_BreakPawns(new List<Pawn>{p}, condition)
                    }
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException("condition");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.LookReference(ref faction, "faction");
            Scribe_Values.LookValue(ref chillSpot, "chillSpot", default(IntVec3));
            Scribe_Values.LookValue(ref checkEventId, "checkEventId", -1);
            Scribe_Values.LookValue(ref stayDuration, "stayDuration", GenDate.TicksPerDay);
        }

        public override StateGraph CreateGraph()
        {
            StateGraph graphArrive = new StateGraph();
            var travelGraph = new LordJob_Travel(chillSpot).CreateGraph();
            travelGraph.StartingToil = new LordToil_CustomTravel(chillSpot, 0.49f, 85); // CHANGED: override StartingToil
            LordToil toilArrive = graphArrive.AttachSubgraph(travelGraph).StartingToil;
            var toilVisit = new LordToil_VisitPoint(chillSpot); // CHANGED
            graphArrive.lordToils.Add(toilVisit);
            LordToil toilTakeWounded = new LordToil_TakeWoundedGuest();
            graphArrive.lordToils.Add(toilTakeWounded);
            StateGraph graphExit = new LordJob_TravelAndExit(IntVec3.Invalid).CreateGraph();
            LordToil toilExit = graphArrive.AttachSubgraph(graphExit).StartingToil;
            LordToil toilLeaveMap = graphExit.lordToils[1];
            LordToil toilLost = new LordToil_End();
            graphExit.AddToil(toilLost);
            Transition t1 = new Transition(toilArrive, toilVisit);
            t1.triggers.Add(new Trigger_Memo("TravelArrived"));
            graphArrive.transitions.Add(t1);
            LordToil_ExitMapBest toilExitCold = new LordToil_ExitMapBest(); // ADDED TOIL
            graphArrive.AddToil(toilExitCold);
            Transition t6 = new Transition(toilArrive, toilExitCold); // ADDED TRANSITION
            t6.triggers.Add(new Trigger_UrgentlyCold());
            t6.preActions.Add(new TransitionAction_Message("MessageVisitorsLeavingCold".Translate(new object[] { faction.Name })));
            t6.preActions.Add(new TransitionAction_Custom(() => StopPawns(lord.ownedPawns)));
            graphArrive.transitions.Add(t6);
            Transition t2 = new Transition(toilVisit, toilTakeWounded);
            t2.triggers.Add(new Trigger_WoundedGuestPresent());
            t2.preActions.Add(new TransitionAction_Message("MessageVisitorsTakingWounded".Translate(new object[] {faction.def.pawnsPlural.CapitalizeFirst(), faction.Name})));
            graphArrive.transitions.Add(t2);
            Transition t3 = new Transition(toilVisit, toilLeaveMap);
            t3.triggers.Add(new Trigger_BecameColonyEnemy());
            t3.preActions.Add(new TransitionAction_WakeAll());
            t3.preActions.Add(new TransitionAction_SetDefendLocalGroup());
            graphArrive.transitions.Add(t3);
            Transition t4 = new Transition(toilArrive, toilExit);
            t4.triggers.Add(new Trigger_BecameColonyEnemy());
            //t4.triggers.Add(new Trigger_VisitorsPleasedMax(MaxPleaseAmount(faction.ColonyGoodwill))); // CHANGED
            t4.triggers.Add(new Trigger_VisitorsAngeredMax(IncidentWorker_VisitorGroup.MaxAngerAmount(faction.PlayerGoodwill))); // CHANGED
            t4.preActions.Add(new TransitionAction_WakeAll());
            t4.preActions.Add(new TransitionAction_EnsureHaveExitDestination());
            graphArrive.transitions.Add(t4);
            Transition t5 = new Transition(toilVisit, toilExit);
            t5.triggers.Add(new Trigger_TicksPassed(stayDuration));
            t5.triggers.Add(new Trigger_SentAway());
            t5.preActions.Add(new TransitionAction_Message("VisitorsLeaving".Translate(new object[] { faction.Name })));
            t5.preActions.Add(new TransitionAction_WakeAll());
            t5.preActions.Add(new TransitionAction_EnsureHaveExitDestination());
            graphArrive.transitions.Add(t5);
            Transition t7 = new Transition(toilArrive, toilExitCold);
            t7.triggers.Add(new Trigger_SentAway());
            t7.preActions.Add(new TransitionAction_Message("VisitorsLeaving".Translate(new object[] { faction.Name })));
            t7.preActions.Add(new TransitionAction_WakeAll());
            t7.preActions.Add(new TransitionAction_Custom(() => StopPawns(lord.ownedPawns)));
            t7.preActions.Add(new TransitionAction_Custom(() => LordToil_VisitPoint.DisplayLeaveMessage(Mathf.InverseLerp(-100, 100, faction.PlayerGoodwill), faction, lord.ownedPawns.Count, Map, true)));
            graphArrive.transitions.Add(t7);

            return graphArrive;
        }

        private static void StopPawns(IEnumerable<Pawn> pawns)
        {
            foreach (var pawn in pawns)
            {
                pawn.pather.StopDead();
                pawn.ClearMind();
            }
        }
    }
}