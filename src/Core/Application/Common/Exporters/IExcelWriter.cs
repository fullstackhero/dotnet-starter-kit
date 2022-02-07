using System.Data;

namespace FSH.WebApi.Application.Common.Exporters;

public interface IExcelWriter : ITransientService
{
    MemoryStream WriteToStream<T>(IList<T> data);
}
