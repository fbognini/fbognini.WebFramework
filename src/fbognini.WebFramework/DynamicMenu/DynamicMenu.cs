using System.Collections.Generic;

namespace fbognini.WebFramework.DynamicMenu
{
    public class DynamicMenu
    {
        public DynamicMenu(List<DynamicMenuGroup> groups)
        {
            Groups = groups;
        }

        public List<DynamicMenuGroup> Groups { get; set; }
    }
}
