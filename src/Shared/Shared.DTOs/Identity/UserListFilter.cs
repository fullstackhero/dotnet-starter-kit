using DN.WebApi.Shared.DTOs.Filters;
using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Identity;

[DataContract]
public class UserListFilter : PaginationFilter
{
    [DataMember(Order = 1)]
    public bool? IsActive { get; set; }
}