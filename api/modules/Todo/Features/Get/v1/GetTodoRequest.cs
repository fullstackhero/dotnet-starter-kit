using MediatR;

namespace FSH.WebApi.Todo.Features.Get.v1;
public class GetTodoRequest : IRequest<GetTodoRepsonse>
{
    public Guid Id { get; set; }
    public GetTodoRequest(Guid id) => Id = id;
}
