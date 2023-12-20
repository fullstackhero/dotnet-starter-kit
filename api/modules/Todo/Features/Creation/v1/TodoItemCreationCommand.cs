using FluentValidation;
using FSH.WebApi.Todo.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FSH.WebApi.Todo.Features.Creation.v1;
public record TodoItemCreationCommand(string? Title, string? Note) : IRequest<Guid>;

public class TodoItemCreationValidator : AbstractValidator<TodoItemCreationCommand>
{
    public TodoItemCreationValidator(TodoDbContext context)
    {
        RuleFor(p => p.Title)
            .NotEmpty()
            .MustAsync(async (title, token) => await context.Todos.AllAsync(a => a.Title != title, token).ConfigureAwait(false))
            .WithMessage((_, name) => "todo item title '{PropertyValue}' already exists.");

        RuleFor(p => p.Note).NotEmpty();
    }
}
