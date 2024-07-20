using MediatR;

namespace FSH.Framework.Core.Storage.File.Features;

public class FileUploadRequestCommand : IRequest<FileUploadResponse>
{
    public string Name { get; set; } = default!;
    public string Extension { get; set; } = default!;
    public string Data { get; set; } = default!;
}

