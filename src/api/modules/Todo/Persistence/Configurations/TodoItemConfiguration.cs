using Finbuckle.MultiTenant;
using FSH.Starter.WebApi.Todo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Starter.WebApi.Todo.Persistence.Configurations;
internal sealed class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.IsMultiTenant();
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(100);
        builder.Property(x => x.Note).HasMaxLength(1000);
    }
}
