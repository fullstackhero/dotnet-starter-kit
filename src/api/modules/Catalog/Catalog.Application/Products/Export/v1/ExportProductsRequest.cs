using FSH.Framework.Core.Paging;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Products.Export.v1;

public record ExportProductsRequest(BaseFilter Filter) : IRequest<byte[]>;
