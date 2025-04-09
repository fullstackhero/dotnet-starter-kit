using FluentValidation;
using FSH.Framework.Auditing.Core.Abstractions;
using FSH.Framework.Auditing.Core.Dtos;
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
    public static RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/users/{userId:guid}/trails", async (
            Guid id,
            IQueryDispatcher dispatcher,
            CancellationToken cancellationToken) =>
        {
            var result = await dispatcher.SendAsync<Query, Response>(new Query(id), cancellationToken);
            return TypedResults.Ok(result);
        })
        .WithName(nameof(GetUserTrails))
        .WithSummary("Get user's audit trail details")
        .RequirePermission("Permissions.AuditTrails.View")
        .WithDescription("Get user's audit trail details.");
    }
    public sealed class Handler(IAuditService auditService) : IQueryHandler<Query, Response>
    {
        public async Task<Response> HandleAsync(Query request, CancellationToken cancellationToken = default)
        {
            var trails = await auditService.GetUserTrailsAsync(request.UserId);
            return new Response(trails);
        }
    }
}
