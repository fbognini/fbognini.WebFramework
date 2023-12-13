using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Policy;

namespace fbognini.WebFramework.DynamicMenu
{
    public class DynamicMenuSubitem
    {
        public string Text { get; set; } = string.Empty;
        public string? Area { get; set; }
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public List<string> Roles { get; internal set; } = new();
        public List<ActionAuthorizationPolicy> Policys { get; internal set; } = new();
        public List<ActionFeaturePolicy> Features { get; internal set; } = new();

        public bool? IsCurrent { get; internal set; }
        public bool ShouldBeCurrent(string? area, string controller, string action) => Area == area && Controller == controller && Action == action;
    
    
    }
}
