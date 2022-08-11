using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.SidebarMenu
{
    public class SidebarMenuGroup
    {
        public SidebarMenuGroup()
        {
            Children = new List<SidebarMenuItem>();
        }

        public string Text { get; set; }
        public List<SidebarMenuItem> Children { get; set; }
    }

    public class SidebarMenuItem
    {
        public SidebarMenuItem()
        {
            Children = new List<SidebarMenuSubItem>();
        }
        public string Text { get; set; }
        public string Icon { get; set; }
        public string Label { get; set; }
        public string Area { get; set; }
        public string Controller { get; set; }
        public List<string> Controllers => Controller.Split(',', StringSplitOptions.TrimEntries).ToList();
        public List<SidebarMenuSubItem> Children { get; set; }
    }
}
