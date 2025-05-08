using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Reviews.Create.v1;

public sealed record CreateReviewCommand(
    string Reviewer,
    string Content,
    int Score,
    DateTime ReviewDate,
    Guid AgencyId) : IRequest<CreateReviewResponse>;
