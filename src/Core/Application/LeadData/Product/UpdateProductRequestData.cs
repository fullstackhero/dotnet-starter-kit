using FL_CRMS_ERP_WEBAPI.Application.Catalog.Products;
using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Product;
public class UpdateProductRequestData : IRequest<Guid>
{
    public Guid Id { get; set; }
    public string? ProductName { get; set; }
    public string? ProductCode { get; set; }
    public Guid? VendorId { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Tax { get; set; }
    public bool? Taxable { get; set; }
    public string? Description { get; set; }

    public class UpdateProductRequestHandler : IRequestHandler<UpdateProductRequestData, Guid>
    {
        // Add Domain Events automatically by using IRepositoryWithEvents
        private readonly IRepositoryWithEvents<ProductDetailsModel> _repository;
        private readonly IStringLocalizer _t;

        public UpdateProductRequestHandler(IRepositoryWithEvents<ProductDetailsModel> repository, IStringLocalizer<UpdateProductRequestHandler> localizer) =>
            (_repository, _t) = (repository, localizer);

        public async Task<Guid> Handle(UpdateProductRequestData request, CancellationToken cancellationToken)
        {
            var product = await _repository.GetByIdAsync(request.Id, cancellationToken);

            _ = product
            ?? throw new NotFoundException(_t["Product {0} Not Found.", request.Id]);

            product.Update(request.ProductName, request.ProductCode, request.VendorId, request.UnitPrice, request.Tax, request.Taxable, request.Description);

            await _repository.UpdateAsync(product, cancellationToken);

            return request.Id;
        }
    }
}
