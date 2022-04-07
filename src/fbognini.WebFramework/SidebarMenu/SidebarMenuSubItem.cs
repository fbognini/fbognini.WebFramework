using System.Collections.Generic;

namespace fbognini.WebFramework.SidebarMenu
{
    public class SidebarMenuSubItem
    {
        public SidebarMenuSubItem()
        {
            Roles = new List<string>();
            Policys = new List<PolicyClass>();
        }

        public string Text { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public List<string> Roles { get; set; }
        public List<PolicyClass> Policys { get; set; }
    }
}
