using UnityEngine;
using Verse;

namespace Hospitality.Utilities
{
    [StaticConstructorOnStartup]
    public static class HospitalityContent
    {
        public static readonly Texture2D VendingPriceUp = ContentFinder<Texture2D>.Get("UI/Commands/VendingPriceUp");
        public static readonly Texture2D VendingPriceDown = ContentFinder<Texture2D>.Get("UI/Commands/VendingPriceDown");
        public static readonly Texture2D VendingPriceAuto = ContentFinder<Texture2D>.Get("UI/Commands/VendingPriceAuto");

    }
}
