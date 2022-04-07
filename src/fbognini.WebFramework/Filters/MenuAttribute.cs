using System;

namespace fbognini.WebFramework.Filters
{
    public class MenuAttribute : Attribute
    {
        public MenuAttribute(string group)
        {
            Group = group;
        }

        public MenuAttribute(string group, string name)
            : this(group)
        {
            Name = name;
        }

        public MenuAttribute(string group, string name, string icon)
            : this(group, name)
        {
            Icon = icon;

        }

        public MenuAttribute(string group, string name, string icon, string label)
            : this(group, name, icon)
        {
            Label = label;
        }

        public string Group { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Label { get; set; }
    }

}
