using Asp.Versioning;
using FluentValidation;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Framework.Web.Realtime;
using FSH.Modules.Chat.Contracts.Authorization;
using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Features.v1.Channels.AddChannelMembers;
using FSH.Modules.Chat.Features.v1.Channels.ArchiveChannel;
using FSH.Modules.Chat.Features.v1.Channels.CreateChannel;
using FSH.Modules.Chat.Features.v1.Channels.DiscoverChannels;
using FSH.Modules.Chat.Features.v1.Channels.FindOrCreateDm;
using FSH.Modules.Chat.Features.v1.Channels.GetChannelById;
using FSH.Modules.Chat.Features.v1.Channels.ListMyChannels;
using FSH.Modules.Chat.Features.v1.Channels.MarkChannelRead;
using FSH.Modules.Chat.Features.v1.Channels.RemoveChannelMember;
using FSH.Modules.Chat.Features.v1.Channels.RestoreChannel;
using FSH.Modules.Chat.Features.v1.Channels.UpdateChannel;
using FSH.Modules.Chat.Features.v1.Messages.DeleteMessage;
using FSH.Modules.Chat.Features.v1.Messages.EditMessage;
using FSH.Modules.Chat.Features.v1.Messages.ListChannelMessages;
using FSH.Modules.Chat.Features.v1.Messages.ListMessageReplies;
using FSH.Modules.Chat.Features.v1.Messages.SendMessage;
using FSH.Modules.Chat.Features.v1.Reactions.AddReaction;
using FSH.Modules.Chat.Features.v1.Reactions.RemoveReaction;
using FSH.Modules.Chat.Features.v1.Search;
using FSH.Modules.Chat.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace FSH.Modules.Chat;

/// <summary>
/// Chat module: Slack-style messaging (DMs + group DMs + named channels). Module Order 800 places
/// it after Notifications (750) so the Notifications module can register integration-event handlers
/// before Chat starts publishing.
/// </summary>
public sealed class ChatModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(ChatPermissions.All);

        builder.Services.AddHeroDbContext<ChatDbContext>();
        builder.Services.AddScoped<IDbInitializer, ChatDbInitializer>();
        builder.Services.AddValidatorsFromAssembly(typeof(ChatModule).Assembly);

        // Realtime adapters consumed by AppHub (BuildingBlocks/Web). These let the shared hub
        // verify channel membership and pre-join channel groups without depending on Chat.
        builder.Services.AddScoped<IChannelMembershipChecker, ChannelMembershipChecker>();
        builder.Services.AddScoped<IUserChannelLookup, UserChannelLookup>();

        // @username resolution for SendMessage. Goes through Identity contracts so the user
        // directory stays the single source of truth.
        builder.Services.AddScoped<IMentionResolver, MentionResolver>();

        builder.Services.AddHealthChecks().AddDbContextCheck<ChatDbContext>(
            name: "db:chat",
            failureStatus: HealthStatus.Unhealthy);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints.MapGroup("api/v{version:apiVersion}/chat")
            .WithTags("Chat")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();

        // Channel reads — literal routes first
        group.MapListMyChannelsEndpoint();           // GET /channels
        group.MapDiscoverChannelsEndpoint();         // GET /channels/discover

        // Channel lifecycle
        group.MapCreateChannelEndpoint();
        group.MapFindOrCreateDmEndpoint();           // POST /dms — literal route comes before /{id}
        group.MapRestoreChannelEndpoint();           // literal /restore must precede catch-alls
        group.MapAddChannelMembersEndpoint();
        group.MapRemoveChannelMemberEndpoint();
        group.MapMarkChannelReadEndpoint();
        group.MapUpdateChannelEndpoint();
        group.MapArchiveChannelEndpoint();
        group.MapGetChannelByIdEndpoint();           // GET /channels/{id} — must follow literal routes

        // Messages
        group.MapListChannelMessagesEndpoint();      // GET /channels/{id}/messages
        group.MapListMessageRepliesEndpoint();       // GET /messages/{id}/replies
        group.MapSendMessageEndpoint();              // POST /channels/{id}/messages
        group.MapEditMessageEndpoint();              // PUT /messages/{id}
        group.MapDeleteMessageEndpoint();            // DELETE /messages/{id}

        // Reactions
        group.MapAddReactionEndpoint();              // POST /messages/{id}/reactions
        group.MapRemoveReactionEndpoint();           // DELETE /messages/{id}/reactions/{emoji}

        // Search
        group.MapSearchMessagesEndpoint();           // GET /search
    }
}
