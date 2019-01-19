using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public class JobGiver_Relax : ThinkNode_JobGiver
    {
        DefMap<JoyGiverDef, float> joyGiverChances;

        public override float GetPriority(Pawn pawn)
        {
            if (pawn.needs?.joy == null)
            {
                Log.Message(pawn.Name.ToStringShort + " needs no joy...");
                return 0f;
            }
            float curLevel = pawn.needs.joy.CurLevel;

            if (curLevel < 0.35f)
            {
                return 6f;
            }
            if(curLevel < 0.9f)
                return 1-curLevel;
            return 0f;
        }

        public override void ResolveReferences ()
        {
            joyGiverChances = new DefMap<JoyGiverDef, float>();
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.CurJob != null)
            {
                //Log.ErrorOnce(pawn.NameStringShort+ " already has a job: "+pawn.CurJob, 4325+pawn.thingIDNumber);
                return pawn.CurJob;
            }
            if (pawn.needs == null) Log.ErrorOnce(pawn.Name.ToStringShort + " has no needs", 3463 + pawn.thingIDNumber);
            if (pawn.needs.joy == null) Log.ErrorOnce(pawn.Name.ToStringShort + " has no joy need", 8585 + pawn.thingIDNumber);
            if (pawn.skills == null) Log.ErrorOnce(pawn.Name.ToStringShort + " has no skills", 22352 + pawn.thingIDNumber);
            if (pawn.GetTimeAssignment() == null) Log.ErrorOnce(pawn.Name.ToStringShort + " has no time assignments", 74564 + pawn.thingIDNumber); 

            var allDefsListForReading = PopulateChances(pawn); // Moved to own function
            for (int j = 0; j < joyGiverChances.Count; j++)
            {
                JoyGiverDef giverDef;
                if (!allDefsListForReading.TryRandomElementByWeight(d => joyGiverChances[d], out giverDef))
                {
                    break;
                }
                Job job = giverDef.Worker.TryGiveJob(pawn);
                if (job != null)
                {
                    return job;
                }
                joyGiverChances[giverDef] = 0f;
            }
            Log.ErrorOnce(pawn.Name.ToStringShort + " did not get a relax job.", 45745 + pawn.thingIDNumber);
            return null;
        }

        private List<JoyGiverDef> PopulateChances(Pawn pawn)
        {
            // From Core
            List<JoyGiverDef> allDefsListForReading = DefDatabase<JoyGiverDef>.AllDefsListForReading;
           
            // ADDED
            if(allDefsListForReading==null){
                Log.Message("AllDefsListForReading == null");
                return new List<JoyGiverDef>();
            } // ^^^
            JoyToleranceSet tolerances = pawn.needs.joy.tolerances;
            foreach (JoyGiverDef joyGiverDef in allDefsListForReading)
            {
                this.joyGiverChances[joyGiverDef] = 0f;

                //if (this.JoyGiverAllowed(joyGiverDef)) REMOVED
                {
                    if (!pawn.needs.joy.tolerances.BoredOf(joyGiverDef.joyKind))
                    {
                        if (joyGiverDef.Worker.MissingRequiredCapacity(pawn) == null)
                        {
                            if (joyGiverDef.pctPawnsEverDo < 1f)
                            {
                                Rand.PushState(pawn.thingIDNumber ^ 63216713);
                                if (Rand.Value >= joyGiverDef.pctPawnsEverDo)
                                {
                                    Rand.PopState();
                                    goto IL_FB;
                                }
                                Rand.PopState();
                            }
                            float tolerance = tolerances[joyGiverDef.joyKind];
                            float factor = Mathf.Pow(1f - tolerance, 5f);
                            factor = Mathf.Max(0.001f, factor);
                            this.joyGiverChances[joyGiverDef] = joyGiverDef.Worker.GetChance(pawn)*factor;
                        }
                    }
                }
                IL_FB:
                ;
            }
            return allDefsListForReading;
        }
    }
}