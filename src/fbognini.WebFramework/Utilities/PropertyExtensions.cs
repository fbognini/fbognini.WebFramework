using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace fbognini.WebFramework.Utilities
{
    public static class PropertyExtensions
    {
        public static string GetDisplayName<T>(this string property)
        {
            MemberInfo propertyInfo = typeof(T).GetProperty(property);
            if (propertyInfo == null)
                return null;


            var displayNameAttribute = propertyInfo.GetCustomAttribute(typeof(DisplayNameAttribute)) as DisplayNameAttribute;
            if (displayNameAttribute != null)
                return displayNameAttribute.DisplayName;

            var displayAttribute = propertyInfo.GetCustomAttribute(typeof(DisplayAttribute)) as DisplayAttribute;
            if (displayAttribute != null)
                return displayAttribute.Name;

            return null;
        }

        public static object GetValue(this string property, object src)
        {
            return src.GetType().GetProperty(property).GetValue(src, null);
        }
    }
}
