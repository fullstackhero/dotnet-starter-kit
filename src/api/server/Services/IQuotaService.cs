using System;
using System.Threading.Tasks;
namespace FSH.Starter.Api.Services;
public interface IQuotaService
{
    Task AssertCanConsumeAsync(Guid tenantId);
    Task ConsumeAsync(Guid tenantId);
}
