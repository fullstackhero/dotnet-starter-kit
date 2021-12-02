using DN.WebApi.Shared.DTOs.Filters;
using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Catalog;

[DataContract]
public class ProductListFilter : PaginationFilter
{
    [DataMember(Order = 1)]
    public Guid? BrandId { get; set; }

    [DataMember(Order = 2)]
    public decimal? MinimumRate { get; set; }

    [DataMember(Order = 3)]
    public decimal? MaximumRate { get; set; }
}