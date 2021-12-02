using System.Runtime.Serialization;

namespace GrpcShared.Models;

[DataContract]
public class DeleteRequestGrpc
{
    [DataMember(Order = 1)]
    public int Id { get; set; }
}
