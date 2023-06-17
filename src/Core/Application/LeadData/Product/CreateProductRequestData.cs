using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Product;
public class CreateProductRequestData : IRequest<DefaultIdType>
{
    public string? ProductName { get; set; }
    public string? ProductCode { get; set; }
    public Guid? VendorId { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Tax { get; set; }
    public bool? Taxable { get; set; }
    public string? Description { get; set; }

}

public class CreateProductRequestHandler : IRequestHandler<CreateProductRequestData, DefaultIdType>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<ProductDetailsModel> _repository;

    public CreateProductRequestHandler(IRepositoryWithEvents<ProductDetailsModel> repository) => _repository = repository;

    public async Task<DefaultIdType> Handle(CreateProductRequestData request, CancellationToken cancellationToken)
    {
        var product = new ProductDetailsModel(request.ProductName, request.ProductCode, request.VendorId, request.UnitPrice, request.Tax, request.Taxable, request.Description);

        await _repository.AddAsync(product, cancellationToken);

        return product.Id;
    }
}
