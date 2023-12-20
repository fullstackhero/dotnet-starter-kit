using FSH.WebApi.Todo.Models;
using Microsoft.EntityFrameworkCore;

namespace FSH.WebApi.Todo.Data;
public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options)
    {
    }

    public DbSet<TodoItem> Todos { get; set; } = null!;
}
