using MediatR;

namespace FSH.Starter.WebApi.Setting.Features.v1.Dimensions;
public sealed record DeleteDimensionCommand(
    Guid Id) : IRequest;



