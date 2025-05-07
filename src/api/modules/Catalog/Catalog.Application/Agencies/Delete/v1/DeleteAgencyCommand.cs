using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Agencies.Delete.v1;

public sealed record DeleteAgencyCommand(Guid Id) : IRequest;
