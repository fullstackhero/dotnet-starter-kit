using DN.WebApi.Domain.Common.Contracts;

namespace DN.WebApi.Application.Common.Specifications;

public class PaginationSpecification<T> : Specification<T>
where T : BaseEntity
{
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = int.MaxValue;
}