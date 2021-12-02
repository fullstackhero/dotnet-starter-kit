using DN.WebApi.Shared.DTOs.Catalog;
using System.Runtime.Serialization;

namespace GrpcShared.Models;

[DataContract]
public class UpdateProductRequestGrpc
{
    [DataMember(Order = 1)]
    public Guid Id { get; set; }

    [DataMember(Order = 2)]
    public UpdateProductRequest Request { get; set; }
}
