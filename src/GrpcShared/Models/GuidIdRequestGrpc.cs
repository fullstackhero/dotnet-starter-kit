using System.Runtime.Serialization;

namespace GrpcShared.Models;

[DataContract]
public class GuidIdRequestGrpc
{
    [DataMember(Order = 1)]
    public Guid Id { get; set; }
}
