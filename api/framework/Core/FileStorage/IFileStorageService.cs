using FSH.Framework.Core.FileStorage.Features;

namespace FSH.Framework.Core.FileStorage;

public interface IFileStorageService
{
    public Task<string> UploadAsync<T>(FileUploadRequestCommand? request, FileType supportedFileType, CancellationToken cancellationToken = default)
    where T : class;

    public void Remove(string? path);
}
