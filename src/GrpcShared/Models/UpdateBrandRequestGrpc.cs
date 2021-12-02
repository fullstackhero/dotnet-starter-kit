using DN.WebApi.Shared.DTOs.Catalog;
using System.Runtime.Serialization;

namespace GrpcShared.Models;

[DataContract]
public class UpdateBrandRequestGrpc
{
    [DataMember(Order = 1)]
    public Guid Id { get; set; }

    [DataMember(Order = 2)]
    public UpdateBrandRequest Request { get; set; }
}
