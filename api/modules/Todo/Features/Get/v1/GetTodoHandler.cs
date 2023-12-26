using FSH.WebApi.Todo.Data;
using FSH.WebApi.Todo.Exceptions;
using MediatR;

namespace FSH.WebApi.Todo.Features.Get.v1;
public sealed class GetTodoHandler(TodoDbContext context) : IRequestHandler<GetTodoRequest, GetTodoRepsonse>
{
    public async Task<GetTodoRepsonse> Handle(GetTodoRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await context.Todos.FindAsync(new object[] { request.Id }, cancellationToken).ConfigureAwait(false);
        if (item == null) throw new TodoItemNotFoundException(request.Id);
        return new GetTodoRepsonse(item.Id, item.Title!, item.Note!);
    }
}
