﻿using MediatR;
using MediatR.Pipeline;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Behaviours
{
    public static class Startup
    {
        public static IServiceCollection AddMediatR(this IServiceCollection services, params Type[] handlerAssemblyMarkerTypes)
        {
            services.AddMediatR(x => x.AsScoped(), handlerAssemblyMarkerTypes);
            return services;
        }

        public static IServiceCollection AddIHttpRequestValidationBehavior(this IServiceCollection services) =>
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IHttpRequestValidationBehavior<,>));
    }
}