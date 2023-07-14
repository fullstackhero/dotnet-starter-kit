using FL_CRMS_ERP_WEBAPI.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.CustomerProduct;

public class CreateCustomerProduct : IRequest<DefaultIdType>
{
    public string? ProductName { get; set; }
}

public class CreateCustomerProductHandler : IRequestHandler<CreateCustomerProduct, DefaultIdType>
{
    private readonly IRepositoryWithEvents<CustomerProductModel> _repository;

    public CreateCustomerProductHandler(IRepositoryWithEvents<CustomerProductModel> repository) => _repository = repository;

    public async Task<DefaultIdType> Handle(CreateCustomerProduct request, CancellationToken cancellationToken)
    {
        var returnValue = new CustomerProductModel(request.ProductName);

        await _repository.AddAsync(returnValue, cancellationToken);

        return returnValue.Id;
    }
}
