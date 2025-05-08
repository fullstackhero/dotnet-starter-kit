using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Reviews.Get.v1;

public sealed record GetReviewRequest(Guid Id) : IRequest<ReviewResponse>;
