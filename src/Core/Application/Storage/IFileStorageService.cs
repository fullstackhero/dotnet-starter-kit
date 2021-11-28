using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Domain.Enums;
using DN.WebApi.Shared.DTOs.Storage;

namespace DN.WebApi.Application.Storage;

public interface IFileStorageService : ITransientService
{
    public Task<string> UploadAsync<T>(FileUploadRequest request, FileType supportedFileType)
    where T : class;
}