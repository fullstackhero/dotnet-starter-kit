using MediatR;

namespace FSH.WebApi.Todo.Features.Creation.v1;
public record TodoCreationCommand(string Title, string Note) : IRequest<TodoCreationRepsonse>;



