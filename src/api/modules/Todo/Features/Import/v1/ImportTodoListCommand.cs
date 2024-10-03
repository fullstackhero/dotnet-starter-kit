using FSH.Framework.Core.DataIO;
using FSH.Framework.Core.Storage.File.Features;
using MediatR;

namespace FSH.Starter.WebApi.Todo.Features.Import.v1;

public record ImportTodoListCommand(FileUploadCommand UploadFile, bool IsUpdate ) : IRequest<ImportResponse>;
