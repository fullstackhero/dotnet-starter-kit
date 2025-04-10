using FluentValidation;
using FSH.Framework.Auditing.Contracts;
using FSH.Framework.Auditing.Core.Abstractions;
using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Shared.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Auditing.Endpoints.v1;
public static class GetUserTrails
{
    public sealed record Query(Guid UserId) : IQuery<Response>;
    public sealed record Response(IReadOnlyList<Trail> AuditTrails);
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }
    public sealed class Handler(IAuditService auditService)
    : IQueryHandler<Query, Response>
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken = default)
        {
            var trails = await auditService.GetUserTrailsAsync(query.UserId);
            return new Response(trails);
        }
    }
    public static RouteHandlerBuilder MapEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/users/{userId:guid}/trails", async (
            Guid userId,
            IQueryDispatcher dispatcher,
            CancellationToken cancellationToken) =>
        {
            var result = await dispatcher.SendAsync<Query, Response>(
                new Query(userId), cancellationToken);

            return TypedResults.Ok(result);
        })
        .WithName("GetUserTrails")
        .WithSummary("Get user's audit trail details")
        .WithDescription("Returns the audit trail details for a specific user.")
        .RequirePermission("Permissions.AuditTrails.View");
    }
}
