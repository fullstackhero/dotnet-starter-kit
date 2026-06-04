namespace FSH.Modules.Chat.Contracts.v1.DTOs;

/// <summary>
/// Member role within a channel. Serialized as its string name (global JsonStringEnumConverter),
/// so the SPA sees "Member"/"Admin". Lives in Contracts because it is part of the published wire
/// contract (ChannelMemberDto).
/// </summary>
public enum ChannelMemberRole
{
    Member = 0,
    Admin = 1,
}
