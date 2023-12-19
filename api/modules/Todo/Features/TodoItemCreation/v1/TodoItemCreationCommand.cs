using FluentValidation;
using MediatR;

namespace Todo.Features.TodoItemCreation.v1;
public record TodoItemCreationCommand(string? Title, string? Note) : IRequest<Guid>;

public class TodoItemCreationValidator : AbstractValidator<TodoItemCreationCommand>
{
    public TodoItemCreationValidator()
    {
        RuleFor(p => p.Title).NotEmpty();
        RuleFor(p => p.Note).NotEmpty().MinimumLength(10).MaximumLength(75);
    }
}
