namespace FL_CRMS_ERP_WEBAPI.Application.Common.Exporters;

public interface IExcelWriter : ITransientService
{
    Stream WriteToStream<T>(IList<T> data);
}