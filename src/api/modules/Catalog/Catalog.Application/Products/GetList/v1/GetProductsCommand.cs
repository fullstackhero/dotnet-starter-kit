using FSH.Framework.Core.Paging;
using FSH.Starter.WebApi.Catalog.Application.Products.Get.v1;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Products.GetList.v1;

public record GetProductsCommand(BaseFilter Filter) : IRequest<List<ProductResponse>>;
