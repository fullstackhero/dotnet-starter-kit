using ProtoBuf;

namespace DN.WebApi.Shared.DTOs.Identity;

[ProtoContract(SkipConstructor = true)]
public record TokenRequest(
    [property: ProtoMember(1)] string Email,
    [property: ProtoMember(2)] string Password);