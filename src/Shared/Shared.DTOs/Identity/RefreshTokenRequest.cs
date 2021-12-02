using ProtoBuf;

namespace DN.WebApi.Shared.DTOs.Identity;

[ProtoContract(SkipConstructor = true)]
public record RefreshTokenRequest(
    [property: ProtoMember(1)] string Token,
    [property: ProtoMember(2)] string RefreshToken);