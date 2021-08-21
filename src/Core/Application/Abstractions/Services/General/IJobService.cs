using System.Linq.Expressions;

namespace DN.WebApi.Application.Abstractions.Services.General
{
    public interface IJobService
    {
        string Enqueue(Expression<Func<Task>> methodCall);
    }
}