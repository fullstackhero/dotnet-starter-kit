using Asp.Versioning;
using FSH.Framework.Core.Context;
using FSH.Framework.Eventing;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Persistence;
using FSH.Framework.Quota;
using FSH.Framework.Storage;
using FSH.Framework.Storage.Local;
using FSH.Framework.Storage.Services;
using FSH.Framework.Web.Modules;
using FSH.Modules.Identity.Authorization;
using FSH.Modules.Identity.Authorization.Jwt;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using FSH.Modules.Identity.Features.v1.Groups.AddUsersToGroup;
using FSH.Modules.Identity.Features.v1.Groups.CreateGroup;
using FSH.Modules.Identity.Features.v1.Groups.DeleteGroup;
using FSH.Modules.Identity.Features.v1.Groups.GetGroupById;
using FSH.Modules.Identity.Features.v1.Groups.GetGroupMembers;
using FSH.Modules.Identity.Features.v1.Groups.GetGroups;
using FSH.Modules.Identity.Features.v1.Groups.RemoveUserFromGroup;
using FSH.Modules.Identity.Features.v1.Groups.UpdateGroup;
using FSH.Modules.Identity.Features.v1.Impersonation.EndImpersonation;
using FSH.Modules.Identity.Features.v1.Impersonation.GetImpersonationGrants;
using FSH.Modules.Identity.Features.v1.Impersonation.RevokeImpersonationGrant;
using FSH.Modules.Identity.Features.v1.Impersonation.StartImpersonation;
using FSH.Modules.Identity.Features.v1.Roles;
using FSH.Modules.Identity.Features.v1.Roles.DeleteRole;
using FSH.Modules.Identity.Features.v1.Roles.GetRoleById;
using FSH.Modules.Identity.Features.v1.Roles.GetRoles;
using FSH.Modules.Identity.Features.v1.Roles.GetRoleWithPermissions;
using FSH.Modules.Identity.Features.v1.Roles.UpdateRolePermissions;
using FSH.Modules.Identity.Features.v1.Roles.UpsertRole;
using FSH.Modules.Identity.Features.v1.Sessions.AdminRevokeAllSessions;
using FSH.Modules.Identity.Features.v1.Sessions.AdminRevokeSession;
using FSH.Modules.Identity.Features.v1.Sessions.GetMySessions;
using FSH.Modules.Identity.Features.v1.Sessions.GetTenantSessions;
using FSH.Modules.Identity.Features.v1.Sessions.GetUserSessions;
using FSH.Modules.Identity.Features.v1.Sessions.RevokeAllSessions;
using FSH.Modules.Identity.Features.v1.Sessions.RevokeSession;
using FSH.Modules.Identity.Features.v1.Tokens.RefreshToken;
using FSH.Modules.Identity.Features.v1.Tokens.TokenGeneration;
using FSH.Modules.Identity.Features.v1.TwoFactor.Disable;
using FSH.Modules.Identity.Features.v1.TwoFactor.Enroll;
using FSH.Modules.Identity.Features.v1.TwoFactor.VerifyEnroll;
using FSH.Modules.Identity.Features.v1.Users.AssignUserRoles;
using FSH.Modules.Identity.Features.v1.Users.ChangePassword;
using FSH.Modules.Identity.Features.v1.Users.ConfirmEmail;
using FSH.Modules.Identity.Features.v1.Users.DeleteUser;
using FSH.Modules.Identity.Features.v1.Users.ForgotPassword;
using FSH.Modules.Identity.Features.v1.Users.GetUserById;
using FSH.Modules.Identity.Features.v1.Users.GetUserGroups;
using FSH.Modules.Identity.Features.v1.Users.GetUserPermissions;
using FSH.Modules.Identity.Features.v1.Users.GetUserProfile;
using FSH.Modules.Identity.Features.v1.Users.GetUserRoles;
using FSH.Modules.Identity.Features.v1.Users.GetUsers;
using FSH.Modules.Identity.Features.v1.Users.RegisterUser;
using FSH.Modules.Identity.Features.v1.Users.ResetPassword;
using FSH.Modules.Identity.Features.v1.Users.SearchUsers;
using FSH.Modules.Identity.Features.v1.Users.SelfRegistration;
using FSH.Modules.Identity.Features.v1.Users.SetProfileImage;
using FSH.Modules.Identity.Features.v1.Users.ToggleUserStatus;
using FSH.Modules.Identity.Features.v1.Users.UpdateUser;
using FSH.Modules.Identity.Services;
using Hangfire;
using Hangfire.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace FSH.Modules.Identity;

public class IdentityModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        FSH.Framework.Shared.Constants.PermissionConstants.Register(
            FSH.Modules.Identity.Contracts.Authorization.IdentityPermissions.All);

        var services = builder.Services;
        services.AddScoped<RolePermissionSyncer>();
        services.AddHostedService<RolePermissionSyncHostedService>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, PathAwareAuthorizationHandler>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<ICurrentUserService>());
        services.AddScoped<ICurrentUserInitializer>(sp => sp.GetRequiredService<ICurrentUserService>());
        services.AddScoped<IRequestContextService, RequestContextService>();
        services.AddScoped<IRequestContext>(sp => sp.GetRequiredService<IRequestContextService>());
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IImpersonationGrantService, ImpersonationGrantService>();

        // User services - focused single-responsibility services
        services.AddTransient<IUserRegistrationService, UserRegistrationService>();
        services.AddTransient<IUserProfileService, UserProfileService>();
        services.AddTransient<IUserStatusService, UserStatusService>();
        services.AddTransient<IUserRoleService, UserRoleService>();
        services.AddTransient<IUserPasswordService, UserPasswordService>();
        services.AddTransient<IUserPermissionService, UserPermissionService>();

        // Facade for backward compatibility
        services.AddTransient<IUserService, UserService>();

        services.AddTransient<IRoleService, RoleService>();
        services.AddHeroStorage(builder.Configuration);
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddHeroDbContext<IdentityDbContext>();
        services.AddEventingCore(builder.Configuration);
        services.AddEventingForDbContext<IdentityDbContext>();
        services.AddIntegrationEventHandlers(typeof(IdentityModule).Assembly);
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<IdentityDbContext>(
                name: "db:identity",
                failureStatus: HealthStatus.Unhealthy);
        services.AddScoped<IDbInitializer, IdentityDbInitializer>();

        // Configure password policy options
        services.Configure<PasswordPolicyOptions>(builder.Configuration.GetSection("PasswordPolicy"));

        // Register password history service
        services.AddScoped<IPasswordHistoryService, PasswordHistoryService>();

        // Register password expiry service
        services.AddScoped<IPasswordExpiryService, PasswordExpiryService>();

        // Register session service and background cleanup
        services.AddScoped<ISessionService, SessionService>();
        services.AddHostedService<SessionCleanupHostedService>();

        // Register group role service for group-derived permissions
        services.AddScoped<IGroupRoleService, GroupRoleService>();

        // Quota gauge: reports live user count per tenant for the Users quota.
        services.AddScoped<IQuotaGaugeProvider, UserCountQuotaGaugeProvider>();

        services.AddIdentity<FshUser, FshRole>(options =>
        {
            options.Password.RequiredLength = IdentityModuleConstants.PasswordLength;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.User.RequireUniqueEmail = true;

            // Account lockout: 5 consecutive failed logins → 15-minute lockout.
            // Applies to newly created users by default. Login flow triggers
            // AccessFailedAsync / IsLockedOutAsync in IdentityService.
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
           .AddEntityFrameworkStores<IdentityDbContext>()
           .AddDefaultTokenProviders();

        //metrics
        services.AddSingleton<IdentityMetrics>();

        services.ConfigureJwtAuth();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/identity")
            .WithTags("Identity")
            .WithApiVersionSet(apiVersionSet);

        // tokens
        group.MapGenerateTokenEndpoint().AllowAnonymous().RequireRateLimiting("auth");
        group.MapRefreshTokenEndpoint().AllowAnonymous().RequireRateLimiting("auth");

        // example Hangfire setup for Identity outbox dispatcher
        var jobManager = endpoints.ServiceProvider.GetService<IRecurringJobManager>();
        if (jobManager is not null)
        {
            jobManager.AddOrUpdate(
                "identity-outbox-dispatcher",
                Job.FromExpression<OutboxDispatcher>(d => d.DispatchAsync(CancellationToken.None)),
                Cron.Minutely(),
                new RecurringJobOptions());
        }

        // roles
        group.MapGetRolesEndpoint();
        group.MapGetRoleByIdEndpoint();
        group.MapDeleteRoleEndpoint();
        group.MapGetRolePermissionsEndpoint();
        group.MapUpdateRolePermissionsEndpoint();
        group.MapCreateOrUpdateRoleEndpoint();

        // users
        group.MapAssignUserRolesEndpoint();
        group.MapChangePasswordEndpoint();
        group.MapConfirmEmailEndpoint().RequireRateLimiting("auth");
        group.MapDeleteUserEndpoint();
        group.MapGetUserByIdEndpoint();
        group.MapGetCurrentUserPermissionsEndpoint();
        group.MapGetMeEndpoint();
        group.MapGetUserRolesEndpoint();
        group.MapGetUsersListEndpoint();
        group.MapSearchUsersEndpoint();
        group.MapRegisterUserEndpoint();
        group.MapForgotPasswordEndpoint().RequireRateLimiting("auth");
        group.MapResetPasswordEndpoint().RequireRateLimiting("auth");
        group.MapSelfRegisterUserEndpoint().RequireRateLimiting("auth");
        group.MapToggleUserStatusEndpoint();
        group.MapUpdateUserEndpoint();
        group.MapSetProfileImageEndpoint();

        // sessions - user endpoints
        group.MapGetMySessionsEndpoint();
        group.MapRevokeSessionEndpoint();
        group.MapRevokeAllSessionsEndpoint();

        // sessions - admin endpoints
        group.MapGetTenantSessionsEndpoint();
        group.MapGetUserSessionsEndpoint();
        group.MapAdminRevokeSessionEndpoint();
        group.MapAdminRevokeAllSessionsEndpoint();

        // groups
        group.MapGetGroupsEndpoint();
        group.MapGetGroupByIdEndpoint();
        group.MapCreateGroupEndpoint();
        group.MapUpdateGroupEndpoint();
        group.MapDeleteGroupEndpoint();
        group.MapGetGroupMembersEndpoint();
        group.MapAddUsersToGroupEndpoint();
        group.MapRemoveUserFromGroupEndpoint();

        // user groups
        group.MapGetUserGroupsEndpoint();

        // impersonation
        group.MapStartImpersonationEndpoint();
        group.MapEndImpersonationEndpoint();
        group.MapGetImpersonationGrantsEndpoint();
        group.MapRevokeImpersonationGrantEndpoint();

        // two-factor authentication (TOTP)
        group.MapEnrollTwoFactorEndpoint();
        group.MapVerifyEnrollTwoFactorEndpoint();
        group.MapDisableTwoFactorEndpoint();
    }
}