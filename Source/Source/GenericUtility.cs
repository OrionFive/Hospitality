using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

internal static class GenericUtility
{
    public static bool IsMeal(this Thing thing)
    {
        return thing.def.ingestible != null && thing.def.ingestible.IsMeal;
    }

    public static Pawn GetAnyRelatedWorldPawn(Func<Pawn, bool> selector, int minImportance)
    {
        // Get all important relations from all colonists
        var importantRelations = from colonist in PawnsFinder.AllMaps_FreeColonistsSpawned.Where(c => !c.Dead)
            from otherPawn in colonist.relations.RelatedPawns
            where !otherPawn.Dead && !otherPawn.Spawned && selector(otherPawn) && otherPawn.IsWorldPawn()
            select new {otherPawn, colonist, relationDef = colonist.GetMostImportantRelation(otherPawn)};

        var dictRelations = new Dictionary<Pawn, float>();

        // Calculate the total importance to colony
        foreach (var relation in importantRelations.Where(r=>r.relationDef.importance >= minImportance))
        {
            if (!dictRelations.ContainsKey(relation.otherPawn))
            {
                dictRelations.Add(relation.otherPawn, relation.relationDef.importance);
            }
            else dictRelations[relation.otherPawn] += relation.relationDef.importance;
        }
        //Log.Message(dictRelations.Count + " distinct pawns:");
        //foreach (var relation in dictRelations)
        //{
        //    Log.Message("- " + relation.Key.Name + ": " + relation.Value +(relation.Key.Faction.leader == relation.Key?" (leader)":""));
        //}

        if (dictRelations.Count > 0)
        {
            var choice = dictRelations.RandomElementByWeight(pair => pair.Value);
            //Log.Message(choice.Key.Name + " with " + choice.Value + " points was chosen.");
            return choice.Key;
        }
        else if (minImportance <= 0)
        {
            Log.Message("Couldn't find any pawn that is related to colony.");
            return null;
        }
        else return GetAnyRelatedWorldPawn(selector, minImportance - 50);
    }
}