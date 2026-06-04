namespace FSH.Modules.Chat.Contracts.v1.DTOs;

/// <summary>
/// Channel kind. Serialized as its string name (global JsonStringEnumConverter), so the SPA
/// sees "DirectMessage"/"GroupMessage"/"Channel". Lives in Contracts because it is part of the
/// published wire contract (ChannelDto).
/// </summary>
public enum ChannelType
{
    DirectMessage = 0,
    GroupMessage = 1,
    Channel = 2,
}
