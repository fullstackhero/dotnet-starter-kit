using System.Net;
using System.Security.Claims;
using System.Text;
using DN.WebApi.Application.Abstractions.Repositories;
using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Application.Exceptions;
using DN.WebApi.Application.Settings;
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Infrastructure.Identity.Services;
using DN.WebApi.Infrastructure.Localizer;
using DN.WebApi.Infrastructure.Middlewares;
using DN.WebApi.Infrastructure.Persistence;
using DN.WebApi.Infrastructure.Persistence.Extensions;
using DN.WebApi.Infrastructure.Persistence.Repositories;
using DN.WebApi.Infrastructure.Services.General;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace DN.WebApi.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            services.AddLocalization();
            services.AddServices(config);
            services.AddDistributedMemoryCache();
            services.AddSettings(config);
            services.AddIdentity(config);
            services.PrepareTenantDatabases<ApplicationDbContext>(config);
            services.AddHangfireServer();
            services.AddRouting(options => options.LowercaseUrls = true);
            services.AddMiddlewares(config);
            services.AddSwaggerDocumentation();
            services.AddCorsPolicy();
            services.AddApiVersioning(config =>
           {
               config.DefaultApiVersion = new ApiVersion(1, 0);
               config.AssumeDefaultVersionWhenUnspecified = true;
               config.ReportApiVersions = true;
           });
            services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();
            return services;
        }

        #region Settings
        internal static IServiceCollection AddSettings(this IServiceCollection services, IConfiguration config)
        {
            services
                .Configure<MailSettings>(config.GetSection(nameof(MailSettings)))
                .Configure<TenantSettings>(config.GetSection(nameof(TenantSettings)))
                .Configure<MiddlewareSettings>(config.GetSection(nameof(MiddlewareSettings)))
                .Configure<CorsSettings>(config.GetSection(nameof(CorsSettings)));
            return services;
        }
        #endregion

        #region Identity
        internal static IServiceCollection AddIdentity(this IServiceCollection services, IConfiguration config)
        {
            services
                .Configure<JwtSettings>(config.GetSection(nameof(JwtSettings)))
                .AddIdentity<ApplicationUser, ApplicationRole>(options =>
                {
                    options.Password.RequiredLength = 6;
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            services.AddJwtAuthentication(config);
            return services;
        }

        internal static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services, IConfiguration config)
        {
            var jwtSettings = services.GetOptions<JwtSettings>(nameof(JwtSettings));
            byte[] key = Encoding.ASCII.GetBytes(jwtSettings.Key);
            services
                .AddAuthentication(authentication =>
                {
                    authentication.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    authentication.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(bearer =>
                {
                    bearer.RequireHttpsMetadata = false;
                    bearer.SaveToken = true;
                    bearer.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        RoleClaimType = ClaimTypes.Role,
                        ClockSkew = TimeSpan.Zero
                    };
                    bearer.Events = new JwtBearerEvents
                    {
                        OnChallenge = context =>
                        {
                            context.HandleResponse();
                            if (!context.Response.HasStarted)
                            {
                                throw new IdentityException("You are not Authorized.", statusCode: HttpStatusCode.Unauthorized);
                            }

                            return Task.CompletedTask;
                        },
                        OnForbidden = context =>
                        {
                            throw new IdentityException("You are not authorized to access this resource.", statusCode: HttpStatusCode.Forbidden);
                        },
                    };
                });
            return services;
        }
        #endregion
        internal static IServiceCollection AddMiddlewares(this IServiceCollection services, IConfiguration config)
        {
            var middlewareSettings = services.GetOptions<MiddlewareSettings>(nameof(MiddlewareSettings));
            services.AddSingleton<ExceptionMiddleware>();
            if (middlewareSettings.EnableLocalization) services.AddSingleton<LocalizationMiddleware>();
            if (middlewareSettings.EnableRequestLogging) services.AddSingleton<RequestLoggingMiddleware>();
            return services;
        }
        #region Swagger
        private static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            return services.AddSwaggerGen(options =>
            {
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

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    Description = "Input your Bearer token in this format - Bearer {your token here} to access this API",
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
                options.MapType<TimeSpan>(() => new OpenApiSchema
                {
                    Type = "string",
                    Nullable = true,
                    Pattern = @"^([0-9]{1}|(?:0[0-9]|1[0-9]|2[0-3])+):([0-5]?[0-9])(?::([0-5]?[0-9])(?:.(\d{1,9}))?)?$",
                    Example = new OpenApiString("02:00:00")
                });
            });
        }
        #endregion

        #region CORS
        private static IServiceCollection AddCorsPolicy(this IServiceCollection services)
        {
            var corsSettings = services.GetOptions<CorsSettings>(nameof(CorsSettings));
            return services.AddCors(opt =>
            {
                opt.AddPolicy("CorsPolicy", policy =>
                {
                    policy.AllowAnyHeader().AllowAnyMethod().WithOrigins(corsSettings.Angular);
                });
            });
        }
        #endregion
    }
}