using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace DN.WebApi.Infrastructure.Swagger;

internal static class Startup
{
    internal static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services, IConfiguration config)
    {
        var settings = config.GetSection(nameof(SwaggerSettings)).Get<SwaggerSettings>();
        if (settings.Enable)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                var info = new OpenApiInfo
                {
                    Title = settings.Title,
                    Version = settings.Version,
                    Description = settings.Description,
                    Contact = new OpenApiContact
                    {
                        Name = settings.ContactName,
                        Email = settings.ContactEmail,
                    },
                    License = new OpenApiLicense
                    {
                        Name = settings.LicenseName,
                    }
                };
                if (!string.IsNullOrEmpty(settings.ContactUrl))
                {
                    info.Contact.Url = new Uri(settings.ContactUrl);
                }

                if (!string.IsNullOrEmpty(settings.LicenseUrl))
                {
                    info.License.Url = new Uri(settings.LicenseUrl);
                }

                options.SwaggerDoc("v1", info);
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (!assembly.IsDynamic)
                    {
                        string xmlFile = $"{assembly.GetName().Name}.xml";
                        string xmlPath = Path.Combine(baseDirectory, xmlFile);
                        if (File.Exists(xmlPath))
                        {
                            options.IncludeXmlComments(xmlPath);
                        }
                    }
                }

                if (config["SecuritySettings:Provider"].Equals("AzureAd", StringComparison.OrdinalIgnoreCase))
                {
                    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                    {
                        Name = "oauth2",
                        Description = "OAuth2.0 Auth Code with PKCE",
                        Type = SecuritySchemeType.OAuth2,
                        Flows = new()
                        {
                            AuthorizationCode = new()
                            {
                                AuthorizationUrl = new Uri(config["SecuritySettings:Swagger:AuthorizationUrl"]),
                                TokenUrl = new Uri(config["SecuritySettings:Swagger:TokenUrl"]),
                                Scopes = new Dictionary<string, string>
                                {
                                    { config["SecuritySettings:Swagger:ApiScope"], "access the api" }
                                }
                            }
                        }
                    });
                    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
                            },
                            new[] { config["SecuritySettings:Swagger:ApiScope"] }
                        }
                    });
                }
                else
                {
                    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Description = "Input your Bearer token to access this API",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "Bearer",
                        BearerFormat = "JWT",
                    });
                    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer",
                                },
                                Scheme = "Bearer",
                                Name = "Bearer",
                                In = ParameterLocation.Header,
                            }, new List<string>()
                        },
                    });
                }

                options.MapType<TimeSpan>(() => new OpenApiSchema
                {
                    Type = "string",
                    Nullable = true,
                    Pattern = @"^([0-9]{1}|(?:0[0-9]|1[0-9]|2[0-3])+):([0-5]?[0-9])(?::([0-5]?[0-9])(?:.(\d{1,9}))?)?$",
                    Example = new OpenApiString("02:00:00")
                });
                options.EnableAnnotations();
                options.OperationFilter<AddTenantIdFilter>();
            });
        }

        return services;
    }

    internal static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app, IConfiguration config)
    {
        if (config.GetValue<bool>("SwaggerSettings:Enable"))
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.DefaultModelsExpandDepth(-1);
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                options.RoutePrefix = "swagger";
                options.DisplayRequestDuration();
                options.DocExpansion(DocExpansion.None);
                if (config["SecuritySettings:Provider"].Equals("AzureAd", StringComparison.OrdinalIgnoreCase))
                {
                    options.OAuthClientId(config["SecuritySettings:Swagger:OpenIdClientId"]);
                    options.OAuthUsePkce();
                    options.OAuthScopeSeparator(" ");
                }
            });
        }

        return app;
    }
}