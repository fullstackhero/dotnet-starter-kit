namespace FSH.Starter.WebApi.Catalog.Application.Reviews.Get.v1;

public sealed record ReviewResponse(
    Guid Id,
    string Reviewer,
    string Content,
    int Score,
    DateTime ReviewDate);
