using System.Collections.Generic;
using RimWorld;
using Verse.AI;

namespace Hospitality
{
    public class JobDriver_BrowseItems : JobDriver_Spectate
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            //this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            //this.FailOn(() => JoyGiver_BuyStuff.IsBuyableNow(pawn, TargetThingA));
            foreach (var toil in base.MakeNewToils()) yield return toil;

        }
    }
}