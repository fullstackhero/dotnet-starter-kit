using FSH.Framework.Core.DataIO;
using FSH.Framework.Core.Storage.File.Features;
using MediatR;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;

public record ImportEntityCodesCommand(FileUploadCommand UploadFile, bool IsUpdate ) : IRequest<ImportResponse>;
