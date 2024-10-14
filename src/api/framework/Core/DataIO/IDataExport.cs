namespace FSH.Framework.Core.DataIO;

public interface IDataExport
{
    byte[] ListToByteArray<T>(IList<T> list);
    Stream WriteToStream<T>(IList<T> data);
    Stream WriteToTemplate<T>(T data, string templateFile, string outputFolder);
}
