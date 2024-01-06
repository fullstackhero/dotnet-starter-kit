using FSH.Framework.Core.Abstraction.Persistence;
using FSH.WebApi.Todo.Exceptions;
using FSH.WebApi.Todo.Models;
using MediatR;

namespace FSH.WebApi.Todo.Features.Get.v1;
public sealed class GetTodoHandler(IRepository<TodoItem> repository) : IRequestHandler<GetTodoRequest, GetTodoRepsonse>
{
    public async Task<GetTodoRepsonse> Handle(GetTodoRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (item == null) throw new TodoItemNotFoundException(request.Id);
        return new GetTodoRepsonse(item.Id, item.Title!, item.Note!);
    }
}
