using System.IO;
using System.Reflection;
using Verse;
using Source = Verse.ModContentPack;

namespace Hospitality.Detouring
{
    /// <summary>
    /// Display human friendly names for mods - this should be Vanilla or CCL
    /// </summary>
    public class ModContentPack : Verse.ModContentPack
    {
        [Detour(typeof(Source), bindingFlags = BindingFlags.Public | BindingFlags.Instance)]
        public override string ToString()
        {
            return Name; // Changed
        }

        public ModContentPack(DirectoryInfo directory, int loadOrder, string name) : base(directory, loadOrder, name) {} // Had to add
    }
}