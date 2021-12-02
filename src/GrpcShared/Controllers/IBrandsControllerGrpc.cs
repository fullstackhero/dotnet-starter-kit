using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Catalog;
using DN.WebApi.Shared.DTOs.Filters;
using GrpcShared.Models;
using ProtoBuf.Grpc;
using System.ServiceModel;

namespace GrpcShared.Controllers;

[ServiceContract]
public interface IBrandsControllerGrpc
{
    [OperationContract]
    Task<PaginatedResult<BrandDto>> SearchAsync(BrandListFilter filter, CallContext context = default);

    [OperationContract]
    Task<Result<Guid>> CreateAsync(CreateBrandRequest request, CallContext context = default);

    [OperationContract]
    Task<Result<Guid>> UpdateAsync(UpdateBrandRequestGrpc request, CallContext context = default);

    [OperationContract]
    Task<Result<Guid>> DeleteAsync(GuidIdRequestGrpc request, CallContext context = default);

    [OperationContract]
    Task<Result<string>> GenerateRandomBrandAsync(GenerateRandomBrandRequest request, CallContext context = default);

    [OperationContract]
    Task<Result<string>> DeleteRandomAsync(CallContext context = default);
}
