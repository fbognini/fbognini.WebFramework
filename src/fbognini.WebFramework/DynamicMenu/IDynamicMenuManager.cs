using fbognini.WebFramework.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Microsoft.FeatureManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.DynamicMenu
{
    public interface IDynamicMenuManager
    {
        Task<List<DynamicMenuGroup>> GetGroupsForClaims(ViewContext viewContext);
        Task<List<DynamicMenuGroup>> GetGroupsForClaims(ActionContext actionContext, IEnumerable<string> claims);
        Task<List<DynamicMenuGroup>> GetGroupsForClaims(RouteData routeData, IEnumerable<string> claims);
        Task<List<DynamicMenuGroup>> GetGroupsForClaims(string? area, string controller, string action, IEnumerable<string> claims);
        Task<List<DynamicMenuGroup>> GetGroupsForClaims(IEnumerable<string> claims);
    }

    internal abstract class BaseDynamicMenuManager
    {
        private readonly DynamicMenu dynamicMenu;
        private readonly IFeatureManager? featureManager;

        public BaseDynamicMenuManager(DynamicMenu dynamicMenu, IFeatureManager? featureManager)
        {
            this.dynamicMenu = dynamicMenu;
            this.featureManager = featureManager;
        }

        public Task<List<DynamicMenuGroup>> GetGroupsForClaims(ViewContext viewContext)
        {
            ArgumentNullException.ThrowIfNull(viewContext, nameof(viewContext));

            var user = viewContext.HttpContext.User;
            var claims = user.Claims.Select(x => x.Value);

            return GetGroupsForClaims(viewContext, claims);
        }

        public Task<List<DynamicMenuGroup>> GetGroupsForClaims(ActionContext actionContext, IEnumerable<string> claims)
        {
            ArgumentNullException.ThrowIfNull(actionContext, nameof(actionContext));

            return GetGroupsForClaims(actionContext.RouteData, claims);
        }

        public Task<List<DynamicMenuGroup>> GetGroupsForClaims(RouteData routeData, IEnumerable<string> claims)
        {
            ArgumentNullException.ThrowIfNull(routeData, nameof(routeData));

            var area = routeData.Values["area"]?.ToString();
            var controller = routeData.Values["controller"]?.ToString();
            var action = routeData.Values["action"]?.ToString();

            return GetGroupsForClaimsWithNullableRoute(area, controller, action, claims);
        }

        public Task<List<DynamicMenuGroup>> GetGroupsForClaims(string? area, string controller, string action, IEnumerable<string> claims)
        {
            ArgumentNullException.ThrowIfNull(controller, nameof(controller));
            ArgumentNullException.ThrowIfNull(action, nameof(action));

            return GetGroupsForClaimsWithNullableRoute(area, controller, action, claims);
        }

        public Task<List<DynamicMenuGroup>> GetGroupsForClaims(IEnumerable<string> claims)
            => GetGroupsForClaimsWithNullableRoute(null, null, null, claims);

        private async Task<List<DynamicMenuGroup>> GetGroupsForClaimsWithNullableRoute(string? area, string? controller, string? action, IEnumerable<string> claims)
        {
            var groups = await GetGroups(claims);

            if (controller == null || action == null)
            {
                return groups;
            }

            foreach (var group in groups)
            {
                foreach (var item in group.Children)
                {
                    item.IsCurrent = item.ShouldBeCurrent(area, controller);

                    foreach (var subitem in item.Children)
                    {
                        subitem.IsCurrent = subitem.ShouldBeCurrent(area, controller, action);
                    }
                }
            }

            return groups;
        }

        private async Task<List<DynamicMenuGroup>> GetGroups(IEnumerable<string> claims)
        {
            var menus = new List<DynamicMenuGroup>();
            foreach (var group in dynamicMenu.Groups)
            {
                var items = new List<DynamicMenuItem>();
                foreach (var configurationItem in group.Children)
                {
                    if (!configurationItem.Children.Any())
                    {
                        continue;
                    }

                    var actions = configurationItem.Children.Where(subitem => subitem.Policys.All(policy => policy.IsPolicysValid(claims))).ToList();
                    if (actions.Count == 0)
                    {
                        continue;
                    }

                    var newItem = new DynamicMenuItem()
                    {
                        Area = configurationItem.Area,
                        Label = configurationItem.Label,
                        Controller = configurationItem.Controller,
                        Icon = configurationItem.Icon,
                        Text = configurationItem.Text,
                        Children = actions
                    };

                    if (featureManager is not null)
                    {
                        var featuredActions = new List<DynamicMenuSubitem>();

                        foreach (var action in actions)
                        {
                            if (await action.Features.AllAsync(async policy => await policy.IsPolicysValid(featureManager)))
                            {
                                featuredActions.Add(action);
                            }
                        }

                        if (featuredActions.Count == 0)
                        {
                            continue;
                        }

                        newItem.Children = featuredActions;
                    }

                    items.Add(newItem);
                }

                if (!items.Any())
                {
                    continue;
                }

                menus.Add(new DynamicMenuGroup()
                {
                    Text = group.Text,
                    Children = items,
                });
            }

            return menus;
        }
    }



    internal class DynamicMenuManagerWithFeatureFlags: BaseDynamicMenuManager, IDynamicMenuManager
    {

        public DynamicMenuManagerWithFeatureFlags(DynamicMenu dynamicMenu, IFeatureManager? featureManager = null)
            : base(dynamicMenu, featureManager)
        {
            if (featureManager is null)
            {
                throw new Exception("Please register IFeatureManager with .AddFeatureManagement() to use .AddDynamicMenuWithFeatureFlags() (otherwise you can use .AddDynamicMenu()");
            }
        }
    }

    internal class DynamicMenuManager : BaseDynamicMenuManager, IDynamicMenuManager
    {
        public DynamicMenuManager(DynamicMenu dynamicMenu)
            : base(dynamicMenu, null)
        {
        }
    }
}
