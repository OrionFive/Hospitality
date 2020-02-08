using Harmony;
using RimWorld;
using Verse;

namespace Hospitality.Harmony {
    /// <summary>
    /// Don't draw the trader question mark on guests.
    /// </summary>
    public class OverlayDrawer_Patch
    {
        [HarmonyPatch(typeof(OverlayDrawer), "DrawOverlay")]
        public class DrawOverlay
        {
            [HarmonyPrefix]
            public static bool Prefix(Thing t, OverlayTypes overlayType)
            {
                var tryingToDrawQuestionMarkOnGuest = overlayType == OverlayTypes.QuestionMark && (t as Pawn).IsGuest();
                return !tryingToDrawQuestionMarkOnGuest;
            }
        }
    }
}
