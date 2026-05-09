using FSH.Framework.Core.Paging;
using FSH.Starter.WebApi.Water.Application.Customers.Get.v1;
using MediatR;

namespace FSH.Starter.WebApi.Water.Application.Customers.Search.v1;

public class SearchCustomersCommand : PaginationFilter, IRequest<PagedList<CustomerResponse>>
{
    public string? FullName { get; set; }
    public string? CustomerCode { get; set; }
    public string? Email { get; set; }
}
