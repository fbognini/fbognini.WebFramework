using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace fbognini.WebFramework.OpenApi;

public static class Startup
{

    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services, IConfiguration configuration, Action<SwaggerGenOptions>? configure = null)
    {
        var settings = configuration.GetSection(nameof(SwaggerSettings)).Get<SwaggerSettings>();
        if (!settings.Enable)
        {
            return services;
        }

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(type => type.ToString());

            if (!string.IsNullOrWhiteSpace(settings.Title))
            {
                var info = new OpenApiInfo()
                {
                    Title = settings.Title,
                    Version = settings.Version,
                    Description = settings.Description,
                    Contact = new OpenApiContact()
                    {
                        Name = settings.ContactName,
                        Email = settings.ContactEmail,
                        Url = !string.IsNullOrWhiteSpace(settings.ContactUrl)
                            ? new Uri(settings.ContactUrl)
                            : null
                    }
                };

                if (settings.License)
                {
                    info.License = new OpenApiLicense()
                    {
                        Name = settings.LicenseName,
                        Url = !string.IsNullOrWhiteSpace(settings.LicenseUrl)
                            ? new Uri(settings.LicenseUrl)
                            : null
                    };
                }

                options.SwaggerDoc("v1", info);
            }

            if (settings.Authentication != null)
            {
                if (settings.Authentication.UseBearerAuthentication)
                {
                    options.AddSecurity(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Description = "Input your Bearer token to access this API",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = JwtBearerDefaults.AuthenticationScheme,
                        BearerFormat = "JWT",
                    });
                }

                if (settings.Authentication.UseApiKeyAuthentication && !string.IsNullOrWhiteSpace(settings.Authentication.ApiKeyHeaderName))
                {
                    options.AddSecurity($"ApiKey [{settings.Authentication.ApiKeyHeaderName}]", new OpenApiSecurityScheme
                    {
                        Name = settings.Authentication.ApiKeyHeaderName,
                        Description = "Input your ApiKey to access this API",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "ApiKeyScheme"
                    });
                }
            }

            options.MapType(typeof(TimeSpan), () => new OpenApiSchema()
            {
                Type = "string",
                Nullable = true,
                Pattern = @"^([0-9]{1}|(?:0[0-9]|1[0-9]|2[0-3])+):([0-5]?[0-9])(?::([0-5]?[0-9])(?:.(\d{1,9}))?)?$",
                Example = new OpenApiString("02:00:00")
            });

            options.DocumentFilter<HideOcelotControllersFilter>();
            options.OperationFilter<SwaggerHeaderAttributeOperationFilter>();

            configure?.Invoke(options);
        });

        return services;
    }

    public static SwaggerGenOptions AddSecurity(this SwaggerGenOptions options, string name, OpenApiSecurityScheme scheme)
    {
        options.AddSecurityDefinition(name, scheme);

        var requirement = new OpenApiSecurityRequirement()
        {
            { 
                new OpenApiSecurityScheme()
                {
                    Reference = new OpenApiReference()
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = name,
                    },
                    In = scheme.In
                }, new List<string>() 
            }
        };
        options.AddSecurityRequirement(requirement);

        return options;
    }

    public static IApplicationBuilder UseOpenApiDocumentation(this IApplicationBuilder app, IConfiguration config, Action<SwaggerUIOptions>? configure = null)
    {
        var settings = config.GetSection(nameof(SwaggerSettings)).Get<SwaggerSettings>();
        if (!settings.Enable)
        {
            return app;
        }

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.DefaultModelsExpandDepth(-1);
            options.DocExpansion(DocExpansion.None);

            configure?.Invoke(options);
        });

        return app;
    }
}