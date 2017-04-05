using System;
using System.Reflection;

namespace Hospitality
{
    // Discarded in favour of HugsLib
    [AttributeUsage(AttributeTargets.Method)]
    internal class DetourOldAttribute : Attribute
    {
        public Type source;
        public BindingFlags bindingFlags;

        public DetourOldAttribute(Type source)
        {
            this.source = source;
        }
    }

}