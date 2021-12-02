using ProtoBuf;

namespace DN.WebApi.Shared.DTOs.Identity;

[ProtoContract(SkipConstructor = true)]
public record TokenResponse(
    [property: ProtoMember(1)] string Token,
    [property: ProtoMember(2)] string RefreshToken,
    [property: ProtoMember(3)] DateTime RefreshTokenExpiryTime);