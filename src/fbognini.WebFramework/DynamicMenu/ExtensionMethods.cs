using fbognini.WebFramework.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace fbognini.WebFramework.DynamicMenu
{

    public static class ExtensionMethods
    {
        public static IServiceCollection AddDynamicMenu(this IServiceCollection services, IConfiguration configuration, string baseNamespace, string sectionName = nameof(DynamicMenu))
        {
            var menu = GetDynamicMenu(configuration, baseNamespace, sectionName);
            services.AddSingleton(menu);
            services.AddSingleton<IDynamicMenuManager, DynamicMenuManager>();

            return services;
        }

        public static IServiceCollection AddDynamicMenuWithFeatureFlags(this IServiceCollection services, IConfiguration configuration, string baseNamespace, string sectionName = nameof(DynamicMenu))
        {
            var menu = GetDynamicMenu(configuration, baseNamespace, sectionName);
            services.AddSingleton(menu);
            services.AddSingleton<IDynamicMenuManager, DynamicMenuManagerWithFeatureFlags>();

            return services;
        }

        [Obsolete("Use services.AddDynamicMenu")]
        public static DynamicMenu LoadDynamicMenu(this IServiceProvider provider, IConfiguration configuration, string baseNamespace, string sectionName = nameof(DynamicMenu))
        {
            return GetDynamicMenu(configuration, baseNamespace, sectionName); 
        }

        private static DynamicMenu GetDynamicMenu(IConfiguration configuration, string baseNamespace, string sectionName)
        {
            var groups = configuration.GetSection(sectionName).Get<List<DynamicMenuGroup>>() ?? new();
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

                        var method = controller.GetMethod(action.Action);
                        if (method == null)
                        {
                            continue;
                        }

                        var (roles, authPolicy, featurePolicy) = GetPolicysAndRoles(controller);

                        action.Roles.AddRange(roles);
                        action.Policys.Add(authPolicy);
                        action.Features.Add(featurePolicy);

                        (roles, authPolicy, featurePolicy) = GetPolicysAndRoles(method);

                        action.Roles.AddRange(roles);
                        action.Policys.Add(authPolicy);
                        action.Features.Add(featurePolicy);
                    }
                }
            }

            return new DynamicMenu(groups);

            static (List<string> Roles, ActionAuthorizationPolicy ActionAuthorizationPolicy, ActionFeaturePolicy FeaturePolicy) GetPolicysAndRoles(MemberInfo type)
            {
                var isAnd = false;
                var policys = new List<string>();

                var authorize = type.GetCustomAttribute(typeof(AuthorizeAttribute)) as AuthorizeAttribute;
                var multipleAuthorize = type.GetCustomAttribute(typeof(MultiplePolicysAuthorizeAttribute)) as MultiplePolicysAuthorizeAttribute;

                var roles = authorize?.Roles != null
                    ? authorize.Roles.Split(",").ToList()
                    : new List<string>();

                if (authorize?.Policy != null)
                {
                    policys.Add(authorize.Policy);
                }

                if (multipleAuthorize?.Policys?.Any() == true)
                {
                    policys.AddRange(multipleAuthorize.Policys);
                    isAnd = multipleAuthorize.IsAnd;
                }

                var featureGate = type.GetCustomAttribute(typeof(FeatureGateAttribute)) as FeatureGateAttribute;


                return (roles, new ActionAuthorizationPolicy(policys, isAnd), new ActionFeaturePolicy(featureGate));
            }
        }


    }
}