using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Domain.Common;
using DN.WebApi.Shared.DTOs.FileStorage;

namespace DN.WebApi.Application.FileStorage;

public interface IFileStorageService : ITransientService
{
    public Task<string> UploadAsync<T>(FileUploadRequest? request, FileType supportedFileType)
    where T : class;
}