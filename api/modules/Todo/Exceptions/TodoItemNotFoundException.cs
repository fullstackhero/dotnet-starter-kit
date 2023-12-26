using FSH.Framework.Core.Exceptions;

namespace FSH.WebApi.Todo.Exceptions;
internal class TodoItemNotFoundException : NotFoundException
{
    public TodoItemNotFoundException(Guid id)
        : base($"todo item with id {id} not found")
    {
    }
}
