using MediatR;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;
public sealed record DeleteEntityCodeCommand(
    Guid Id) : IRequest;



