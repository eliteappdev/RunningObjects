using System;
using System.Linq;
using System.Reflection;

namespace RunningObjects.Core
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ShowMethodsAttribute : Attribute
    {
        public static bool Contains(PropertyInfo property)
        {
            return property.GetCustomAttributes(true).OfType<ShowMethodsAttribute>().Any();
        }
    }
}
