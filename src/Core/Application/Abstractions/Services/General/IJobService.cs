using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DN.WebApi.Application.Abstractions.Services.General
{
    public interface IJobService : ITransientService
    {
        string Enqueue(Expression<Func<Task>> methodCall);
    }
}