using Harmony;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
    /// <summary>
    /// Show "Colony" in social tab for player pawns
    /// </summary>
    public class SocialCardUtility_Patch
    {
        [HarmonyPatch(typeof(SocialCardUtility), "GetPawnSituationLabel")]
        public class GetPawnSituationLabel
        {
            [HarmonyPrefix]
            public static bool Replacement(ref string __result, Pawn pawn, Pawn fromPOV)
            {
                if (pawn.Dead)
                {
                    __result = "Dead".Translate();
                    return false;
                }
                if (pawn.Destroyed)
                {
                    __result = "Missing".Translate();
                    return false;
                }
                if (PawnUtility.IsKidnappedPawn(pawn))
                {
                    __result = "Kidnapped".Translate();
                    return false;
                }
                if (pawn.kindDef == PawnKindDefOf.Slave)
                {
                    __result = "Slave".Translate();
                    return false;
                }
                if (PawnUtility.IsFactionLeader(pawn))
                {
                    __result = "FactionLeader".Translate();
                    return false;
                }
                Faction faction = pawn.Faction;
                if (faction == fromPOV.Faction)
                {
                    __result = string.Empty;
                    return false;
                }
                if (faction == null || fromPOV.Faction == null)
                {
                    __result = "Neutral".Translate();
                    return false;
                }

                #region ADDED
                if (faction == Faction.OfPlayer)
                {
                    __result = "Colony".Translate();
                    return false;
                }
                #endregion

                switch (faction.RelationKindWith(fromPOV.Faction))
                {
                    case FactionRelationKind.Hostile:
                        __result = "Hostile".Translate() + ", " + faction.Name;
                        return false;
                    case FactionRelationKind.Neutral:
                        __result = "Neutral".Translate() + ", " + faction.Name;
                        return false;
                    case FactionRelationKind.Ally:
                        __result = "Ally".Translate() + ", " + faction.Name;
                        return false;
                    default:
                        __result = "";
                        return false;
                }
            }
        }
    }
}