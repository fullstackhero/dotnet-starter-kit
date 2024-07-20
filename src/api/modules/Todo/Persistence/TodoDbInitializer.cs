using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Todo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Todo.Persistence;
internal sealed class TodoDbInitializer(
    ILogger<TodoDbInitializer> logger,
    TodoDbContext context) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[{Tenant}] applied database migrations for todo module", context.TenantInfo!.Identifier);
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        const string Title = "Hello World!";
        const string Note = "This is your first task";
        if (await context.Todos.FirstOrDefaultAsync(t => t.Title == Title, cancellationToken).ConfigureAwait(false) is null)
        {
            var todo = TodoItem.Create(Title, Note);
            await context.Todos.AddAsync(todo, cancellationToken);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[{Tenant}] seeding default todo data", context.TenantInfo!.Identifier);
        }
    }
}
