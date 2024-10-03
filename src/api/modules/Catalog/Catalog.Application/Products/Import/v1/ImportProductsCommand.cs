using System.Collections.Concurrent;
using FSH.Framework.Core.DataIO;
using FSH.Framework.Core.Storage.File.Features;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Products.Import.v1;

public record ImportProductsCommand(FileUploadCommand UploadFile, bool IsUpdate ) : IRequest<ImportResponse>;
