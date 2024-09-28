using System.Net;
using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Storage.File.Features;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Products.Export.v1;

public record ExportProductsCommand(BaseFilter Filter) : IRequest<byte[]>;
