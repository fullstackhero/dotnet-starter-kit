using FSH.Framework.Core.Storage.File.Features;
using FSH.Framework.Core.Storage.File;

namespace FSH.Framework.Core.Storage;

public interface IStorageService
{
    public Task<string> UploadAsync<T>(FileUploadRequestCommand? request, FileType supportedFileType, CancellationToken cancellationToken = default)
    where T : class;

    public void Remove(string? path);
}
