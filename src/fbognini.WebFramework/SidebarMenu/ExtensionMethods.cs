using fbognini.WebFramework.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace fbognini.WebFramework.SidebarMenu
{

    public static class ExtensionMethods
    {
        public static SidebarMenu LoadSidebarMenu(this IServiceProvider provider, IConfiguration configuration, string baseNamespace)
        {
            var groups = configuration.GetSection(nameof(SidebarMenu)).Get<List<SidebarMenuGroup>>();
            foreach (var group in groups)
            {
                foreach (var groupChild in group.Children)
                {
                    foreach (var action in groupChild.Children)
                    {
                        var typeNamespace = string.IsNullOrWhiteSpace(action.Area)
                            ? $"{baseNamespace}.Controllers.{action.Controller}Controller"
                            : $"{baseNamespace}.Areas.{action.Area}.Controllers.{action.Controller}Controller";

                        var controller = AppDomain.CurrentDomain.GetAssemblies()
                            .Select(assembly => assembly.GetType(typeNamespace)).FirstOrDefault(t => t != null);

                        if (controller == null)
                        {
                            continue;
                        }

                        var (isAnd, policys, roles) = GetPolicysAndRoles(controller);

                        var method = controller.GetMethod(action.Action);
                        if (method == null)
                        {
                            continue;
                        }

                        action.Roles.AddRange(roles);
                        action.Policys.Add(new PolicyClass(policys, isAnd));

                        (isAnd, policys, roles) = GetPolicysAndRoles(method);

                        action.Roles.AddRange(roles);
                        action.Policys.Add(new PolicyClass(policys, isAnd));
                    }
                }
            }

            return new SidebarMenu()
            {
                Groups = groups
            };


            (bool, List<string>, List<string>) GetPolicysAndRoles(MemberInfo type)
            {
                bool isAnd = false;
                List<string> policys = new List<string>();

                var authorize = type.GetCustomAttribute(typeof(AuthorizeAttribute)) as AuthorizeAttribute;
                var multipleAuthorize = type.GetCustomAttribute(typeof(MultiplePolicysAuthorizeAttribute)) as MultiplePolicysAuthorizeAttribute;

                if (authorize?.Policy != null)
                {
                    policys.Add(authorize.Policy);
                }
                if (multipleAuthorize?.Policys?.Any() == true)
                {
                    policys.AddRange(multipleAuthorize.Policys);
                    isAnd = multipleAuthorize.IsAnd;
                }

                List<string> roles = authorize?.Roles != null
                    ? authorize.Roles.Split(",").ToList()
                    : new List<string>();

                return (isAnd, policys, roles);
            }
        }
    }
}