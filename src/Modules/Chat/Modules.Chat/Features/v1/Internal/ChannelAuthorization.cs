using FSH.Framework.Core.Exceptions;
using FSH.Modules.Chat.Contracts.v1.DTOs;
using FSH.Modules.Chat.Domain;
using FSH.Modules.Chat.Localization;

namespace FSH.Modules.Chat.Features.v1.Internal;

/// <summary>
/// Small assertion helpers used by channel/message handlers so the rules stay in one place.
/// Throws framework-aware exceptions so the global handler emits the right HTTP status.
/// </summary>
internal static class ChannelAuthorization
{
    public static ChannelMember RequireMember(this ChatChannel channel, string userId)
    {
        var member = channel.Members.FirstOrDefault(m => string.Equals(m.UserId, userId, StringComparison.Ordinal));
        // Use NotFoundException (404) instead of Forbidden so non-members can't probe channel existence.
        return member ?? throw new NotFoundException("Channel not found.")
        {
            MessageKey = "Chat.ChannelNotFound",
            ResourceSource = typeof(ChatResources),
        };
    }

    public static ChannelMember RequireAdmin(this ChatChannel channel, string userId)
    {
        var member = channel.RequireMember(userId);
        if (member.Role != ChannelMemberRole.Admin)
        {
            throw new ForbiddenException("Channel admin role required.")
            {
                MessageKey = "Chat.ChannelAdminRoleRequired",
                ResourceSource = typeof(ChatResources),
            };
        }
        return member;
    }
}
