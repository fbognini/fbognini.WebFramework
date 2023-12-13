using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.DynamicMenu
{
    public class DynamicMenuGroup
    {

        public string Text { get; set; } = string.Empty;
        public List<DynamicMenuItem> Children { get; set; } = new();
    }

    public class DynamicMenuItem
    {
        public string Text { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string? Area { get; set; }
        public string Controller { get; set; } = string.Empty;
        public List<string> Controllers => Controller.Split(',', StringSplitOptions.TrimEntries).ToList();

        public List<DynamicMenuSubitem> Children { get; set; } = new();

        public bool? IsCurrent { get; internal set; }
        public bool ShouldBeCurrent(string? area, string controller) => Area == area && Controllers.Contains(controller);
    }
}
