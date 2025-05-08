using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Reviews.Delete.v1;

public sealed record DeleteReviewCommand(Guid Id) : IRequest;