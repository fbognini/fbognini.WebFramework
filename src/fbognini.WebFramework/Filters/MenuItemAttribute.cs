using System;

namespace fbognini.WebFramework.Filters
{
    public class MenuItemAttribute : Attribute
    {
        public MenuItemAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}
