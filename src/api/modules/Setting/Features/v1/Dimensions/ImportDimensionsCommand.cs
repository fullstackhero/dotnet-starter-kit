using FSH.Framework.Core.DataIO;
using FSH.Framework.Core.Storage.File.Features;
using MediatR;

namespace FSH.Starter.WebApi.Setting.Features.v1.Dimensions;

public record ImportDimensionsCommand(FileUploadCommand UploadFile, bool IsUpdate ) : IRequest<ImportResponse>;
