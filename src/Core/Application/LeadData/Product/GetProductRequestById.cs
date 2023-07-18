using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Product;
public class GetProductRequestById : IRequest<ProductDtoData>
{
    public Guid Id { get; set; }

    public GetProductRequestById(Guid id) => Id = id;
}

public class ProductByIdSpec : Specification<ProductDetailsModel, ProductDtoData>, ISingleResultSpecification
{
    public ProductByIdSpec(Guid id) =>
        Query.Where(p => p.Id == id);
}

public class GetProductRequestHandler : IRequestHandler<GetProductRequestById, ProductDtoData>
{
    private readonly IRepository<ProductDetailsModel> _repository;
    private readonly IStringLocalizer _t;

    public GetProductRequestHandler(IRepository<ProductDetailsModel> repository, IStringLocalizer<GetProductRequestHandler> localizer) => (_repository, _t) = (repository, localizer);

    public async Task<ProductDtoData> Handle(GetProductRequestById request, CancellationToken cancellationToken) =>
        await _repository.GetBySpecAsync(
            (ISpecification<ProductDetailsModel, ProductDtoData>)new ProductByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["Product {0} Not Found.", request.Id]);
}