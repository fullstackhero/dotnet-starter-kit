using FSH.Framework.Infrastructure.Persistence;
using FSH.WebApi.Todo.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FSH.WebApi.Todo.Persistence;
public class TodoDbContext : FshDbContext
{
    public TodoDbContext(DbContextOptions options, IPublisher publisher)
        : base(options, publisher)
    {
    }

    public DbSet<TodoItem> Todos { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TodoDbContext).Assembly);
    }
}
