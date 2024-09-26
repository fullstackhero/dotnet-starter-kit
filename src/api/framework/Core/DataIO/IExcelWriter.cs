namespace FSH.Framework.Core.DataIO;

public interface IExcelWriter
{
    Stream WriteToStream<T>(IList<T> data);
    Stream WriteToTemplate<T>(T data, string templateFile);
}
