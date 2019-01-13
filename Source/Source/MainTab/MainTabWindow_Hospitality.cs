using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Hospitality.MainTab
{
    public class MainTabWindow_Hospitality : MainTabWindow_PawnTable
    {
        private static PawnTableDef pawnTableDef;

        protected override PawnTableDef PawnTableDef => pawnTableDef ?? (pawnTableDef = DefDatabase<PawnTableDef>.GetNamed("Guests"));

        protected override IEnumerable<Pawn> Pawns => Find.CurrentMap.mapPawns.AllPawns.Where(p => p.IsGuest());

        public override void PostOpen()
        {
            base.PostOpen();
            Find.World.renderer.wantedMode = WorldRenderMode.None;
        }
    }
}
