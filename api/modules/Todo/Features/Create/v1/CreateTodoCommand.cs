using System.ComponentModel;
using MediatR;

namespace FSH.WebApi.Todo.Features.CreateTodo.v1;
public record CreateTodoCommand(
    [property: DefaultValue("Hello World!")] string Title,
    [property: DefaultValue("Important Note.")] string Note) : IRequest<CreateTodoRepsonse>;



