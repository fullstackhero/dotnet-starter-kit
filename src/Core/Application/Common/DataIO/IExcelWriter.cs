namespace FSH.WebApi.Application.Common.DataIO;

public interface IExcelWriter : ITransientService
{
    Stream WriteToStream<T>(IList<T> data);
}