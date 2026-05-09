using MediatR;

namespace FSH.Starter.WebApi.Water.Application.Tariffs.Delete.v1;

public sealed record DeleteTariffCommand(Guid Id) : IRequest;
