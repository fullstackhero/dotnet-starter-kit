using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Auditing;
using ProtoBuf.Grpc;
using System.ServiceModel;

namespace GrpcShared.Controllers;

[ServiceContract]
public interface IAuditLogsControllerGrpc
{
    [OperationContract]
    public Task<IResult<IEnumerable<AuditResponse>>> GetMyLogsAsync(CallContext context = default);
}
