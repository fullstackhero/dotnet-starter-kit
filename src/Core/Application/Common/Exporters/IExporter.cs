using System.Data;

namespace FSH.WebApi.Application.Common.Exporters;

public interface IExporter : ITransientService
{
    MemoryStream ExportToAsync(DataTable dt);
    DataTable Convert<T>(IList<T> data);
}
