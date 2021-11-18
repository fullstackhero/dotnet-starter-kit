using System.Threading.Tasks;
using DN.WebApi.Domain.Contracts;

namespace DN.WebApi.Application.Abstractions.Services.General
{
    public interface IEventService : ITransientService
    {
        Task PublishAsync(DomainEvent domainEvent);
    }
}