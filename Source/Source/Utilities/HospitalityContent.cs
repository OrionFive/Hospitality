using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Hospitality.Utilities
{
    [StaticConstructorOnStartup]
    public static class HospitalityContent
    {
        public static Texture2D VendingPriceUp = ContentFinder<Texture2D>.Get("UI/Commands/VendingPriceUp");
        public static Texture2D VendingPriceDown = ContentFinder<Texture2D>.Get("UI/Commands/VendingPriceDown");
        public static Texture2D VendingPriceAuto = ContentFinder<Texture2D>.Get("UI/Commands/VendingPriceAuto");


    }
}
