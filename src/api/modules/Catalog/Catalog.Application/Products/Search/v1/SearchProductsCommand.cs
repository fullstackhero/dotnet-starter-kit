using FSH.Framework.Core.Paging;
using FSH.Starter.WebApi.Catalog.Application.Products.Get.v1;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Products.Search.v1;

public record SearchProductsCommand(PaginationFilter filter) : IRequest<PagedList<ProductResponse>>;
