using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
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

        public override bool NeverInRestraints { get { return true; } }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref chillSpot, "chillSpot");
            Scribe_Values.Look(ref checkEventId, "checkEventId", -1);
            Scribe_Values.Look(ref stayDuration, "stayDuration", GenDate.TicksPerDay);
        }

        public override StateGraph CreateGraph()
        {
            StateGraph graphArrive = new StateGraph();
            StateGraph graphExit = new LordJob_TravelAndExit(IntVec3.Invalid).CreateGraph();
            StateGraph travelGraph = new LordJob_Travel(chillSpot).CreateGraph();
            travelGraph.StartingToil = new LordToil_CustomTravel(chillSpot, 0.49f, 85);
            // Arriving
            LordToil toilArriving = graphArrive.AttachSubgraph(travelGraph).StartingToil;
            // Visiting
            var toilVisiting = new LordToil_VisitPoint();
            graphArrive.lordToils.Add(toilVisiting);
            // Exit
            LordToil toilExit = graphArrive.AttachSubgraph(graphExit).StartingToil;
            // Leave map
            LordToil toilLeaveMap = graphExit.lordToils[1];
            // Take wounded
            LordToil toilTakeWounded = new LordToil_TakeWoundedGuest();
            graphExit.AddToil(toilTakeWounded);
            // Exit from cold
            LordToil_ExitMap toilExitCold = new LordToil_ExitMap();
            graphArrive.AddToil(toilExitCold);
            // Arrived
            Transition t1 = new Transition(toilArriving, toilVisiting);
            t1.triggers.Add(new Trigger_Memo("TravelArrived"));
            graphArrive.transitions.Add(t1);
            // Too cold / hot
            Transition t6 = new Transition(toilArriving, toilExitCold);
            t6.AddTrigger(new Trigger_PawnExperiencingDangerousTemperatures());
            t6.AddPreAction(new TransitionAction_Message("MessageVisitorsDangerousTemperature".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name)));
            t6.AddPreAction(new TransitionAction_EnsureHaveExitDestination());
            t6.AddPostAction(new TransitionAction_EndAllJobs());
            graphArrive.AddTransition(t6);
            // Became enemy while arriving
            Transition t3 = new Transition(toilVisiting, toilLeaveMap);
            t3.triggers.Add(new Trigger_BecamePlayerEnemy());
            t3.postActions.Add(new TransitionAction_WakeAll());
            t3.preActions.Add(new TransitionAction_SetDefendLocalGroup());
            graphArrive.transitions.Add(t3);
            // Leave if became angry
            Transition t4 = new Transition(toilArriving, toilExit);
            t4.triggers.Add(new Trigger_BecamePlayerEnemy());
            t4.triggers.Add(new Trigger_VisitorsAngeredMax(IncidentWorker_VisitorGroup.MaxAngerAmount(faction.PlayerGoodwill)));
            t4.postActions.Add(new TransitionAction_WakeAll());
            t4.preActions.Add(new TransitionAction_EnsureHaveExitDestination());
            graphArrive.transitions.Add(t4);
            // Leave if stayed long enough
            Transition t5 = new Transition(toilVisiting, toilExit);
            t5.triggers.Add(new Trigger_TicksPassedAndOkayToLeave(stayDuration));
            t5.triggers.Add(new Trigger_SentAway());
            t5.preActions.Add(new TransitionAction_Message("VisitorsLeaving".Translate(faction.Name)));
            t5.postActions.Add(new TransitionAction_WakeAll());
            t5.preActions.Add(new TransitionAction_EnsureHaveExitDestination());
            graphArrive.transitions.Add(t5);
            // Leave if sent away
            Transition t7 = new Transition(toilArriving, toilExitCold);
            t7.triggers.Add(new Trigger_SentAway());
            t7.preActions.Add(new TransitionAction_Message("VisitorsLeaving".Translate(faction.Name)));
            t7.postActions.Add(new TransitionAction_WakeAll());
            t7.postActions.Add(new TransitionAction_Custom(() => StopPawns(lord.ownedPawns)));
            t7.preActions.Add(new TransitionAction_Custom(() => LordToil_VisitPoint.DisplayLeaveMessage(Mathf.InverseLerp(-100, 100, faction.PlayerGoodwill), faction, lord.ownedPawns.Count, Map, true)));
            graphArrive.transitions.Add(t7);
            // Take wounded guest when leaving
            Transition t8 = new Transition(toilExit, toilTakeWounded);
            t8.AddTrigger(new Trigger_WoundedGuestPresent());
            t8.AddPreAction(new TransitionAction_Message("MessageVisitorsTakingWounded".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name)));
            graphExit.AddTransition(t8);

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

        public override void Notify_PawnLost(Pawn pawn, PawnLostCondition condition)
        {
            if (condition == PawnLostCondition.ExitedMap) return;

            //Log.Message("lord owns "+lord.ownedPawns.Select(p=>p.LabelShort).ToCommaList());
            if (!lord.ownedPawns.Any())
            {
                GuestUtility.OnLostEntireGroup(lord);
            }
        }
    }
}