using DN.WebApi.Application.Common.Interfaces;
using System.ComponentModel;

namespace DN.WebApi.Application.Catalog.Interfaces;

public interface IBrandGeneratorJob : IScopedService
{
    [DisplayName("Generate Random Brand example job on Queue notDefault")]
    Task GenerateAsync(int nSeed);

    [DisplayName("removes all radom brands created example job on Queue notDefault")]
    Task CleanAsync();
}