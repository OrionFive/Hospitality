using System.IO;
using System.Reflection;
using HugsLib.Source.Detour;
using Verse;
using Source = Verse.ModContentPack;

namespace Hospitality.Detouring
{
    /// <summary>
    /// Display human friendly names for mods - this should be Vanilla or CCL
    /// </summary>
    public class ModContentPack : Verse.ModContentPack
    {
        [DetourMethod(typeof(Source), "ToString")]
        public override string ToString()
        {
            return Name; // Changed
        }

        public ModContentPack(DirectoryInfo directory, int loadOrder, string name) : base(directory, loadOrder, name) {} // Had to add
    }
}