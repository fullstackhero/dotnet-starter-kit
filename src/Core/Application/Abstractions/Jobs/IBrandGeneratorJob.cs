using DN.WebApi.Application.Abstractions.Services;
using System.Threading.Tasks;

namespace DN.WebApi.Application.Abstractions.Jobs
{
    public interface IBrandGeneratorJob : IScopedService
    {
        Task GenerateAsync(int nSeed);
        Task CleanAsync();
    }
}
