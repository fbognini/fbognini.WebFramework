using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
