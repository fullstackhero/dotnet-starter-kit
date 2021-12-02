using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Catalog;
using GrpcShared.Models;
using ProtoBuf.Grpc;
using System.ServiceModel;

namespace GrpcShared.Controllers;

[ServiceContract]
public interface IProductsControllerGrpc
{
    [OperationContract]
    public Task<Result<ProductDetailsDto>> GetAsync(GuidIdRequestGrpc request, CallContext context = default);

    [OperationContract]
    public Task<Result<ProductDto>> GetDapperAsync(GuidIdRequestGrpc request, CallContext context = default);

    [OperationContract]
    public Task<PaginatedResult<ProductDto>> SearchAsync(ProductListFilter filter, CallContext context = default);

    [OperationContract]
    public Task<Result<Guid>> CreateAsync(CreateProductRequest request, CallContext context = default);

    [OperationContract]
    public Task<Result<Guid>> UpdateAsync(UpdateProductRequestGrpc request, CallContext context = default);

    [OperationContract]
    public Task<Result<Guid>> DeleteAsync(GuidIdRequestGrpc request, CallContext context = default);
}
