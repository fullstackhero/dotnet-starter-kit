namespace FSH.Starter.Blazor.Client.Components.EntityTable;

public interface IImportModal<out TRequest>
{
    TRequest RequestModel { get; }
    void ForceRender();
}
