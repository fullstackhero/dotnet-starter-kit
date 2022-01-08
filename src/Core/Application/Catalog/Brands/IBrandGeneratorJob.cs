using System.ComponentModel;
using DN.WebApi.Application.Common.Interfaces;

namespace DN.WebApi.Application.Catalog.Brands;

public interface IBrandGeneratorJob : IScopedService
{
    [DisplayName("Generate Random Brand example job on Queue notDefault")]
    Task GenerateAsync(int nSeed, CancellationToken cancellationToken);

    [DisplayName("removes all radom brands created example job on Queue notDefault")]
    Task CleanAsync(CancellationToken cancellationToken);
}