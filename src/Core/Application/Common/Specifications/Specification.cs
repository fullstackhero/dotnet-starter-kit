using DN.WebApi.Application.Common.Models;
using DN.WebApi.Domain.Common.Contracts;

namespace DN.WebApi.Application.Common.Specifications;

public class Specification<T> : BaseSpecification<T>
where T : BaseEntity
{
    public string? Keyword { get; set; }
    public Search? AdvancedSearch { get; set; }
    public Filters<T>? Filters { get; set; }
}
