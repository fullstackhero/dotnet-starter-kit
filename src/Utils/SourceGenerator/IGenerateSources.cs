using Microsoft.Extensions.Configuration;

namespace FSH.WebApi.Utils.SourceGenerator;

public interface IGenerateSources
{
    Task InitializeAsync(CancellationToken cancellationToken);
}