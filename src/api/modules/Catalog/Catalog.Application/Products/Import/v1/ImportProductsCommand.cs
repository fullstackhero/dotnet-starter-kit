using FSH.Framework.Core.Storage.File.Features;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Products.Import.v1;

public record ImportProductsCommand(FileUploadCommand UploadFile) : IRequest<int>;
