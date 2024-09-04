using MediatR;

namespace FSH.Starter.WebApi.Setting.Dimension.Features.v1;
public sealed record DeleteDimensionCommand(
    Guid Id) : IRequest;



