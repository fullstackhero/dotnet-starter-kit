using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Reviews.Update.v1;

public sealed record UpdateReviewCommand(
    Guid Id,
    string? Reviewer,
    string? Content,
    int? Score,
    DateTime? ReviewDate,
    Guid AgencyId) : IRequest<UpdateReviewResponse>;
