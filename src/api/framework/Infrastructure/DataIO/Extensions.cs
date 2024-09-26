using FSH.Framework.Core.DataIO;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Infrastructure.DataIO;

internal static class Extensions
{
    internal static IServiceCollection ConfigureDataImportExport(this IServiceCollection services)
    {
        services.AddTransient<IExcelWriter, ExcelWriter>();
        services.AddTransient<IExcelReader, ExcelReader>();

        return services;
    }
}
