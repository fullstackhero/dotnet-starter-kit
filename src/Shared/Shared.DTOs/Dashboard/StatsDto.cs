using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Dashboard;

[DataContract]
public class StatsDto
{
    [DataMember(Order = 1)]
    public int ProductCount { get; set; }

    [DataMember(Order = 2)]
    public int BrandCount { get; set; }

    [DataMember(Order = 3)]
    public int UserCount { get; set; }

    [DataMember(Order = 4)]
    public int RoleCount { get; set; }
}