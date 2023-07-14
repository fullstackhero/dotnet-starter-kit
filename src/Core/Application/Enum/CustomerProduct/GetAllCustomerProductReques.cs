using FL_CRMS_ERP_WEBAPI.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.CustomerProduct;

public class GetAllCustomerProductReques : IRequest<List<CustomerProductModel>>
{
    public GetAllCustomerProductReques()
    {
    }

    public class GetAllCustomerProductRequesHandler : IRequestHandler<GetAllCustomerProductReques, List<CustomerProductModel>>
    {
        private readonly IRepositoryWithEvents<CustomerProductModel> _repository;

        public GetAllCustomerProductRequesHandler(IRepositoryWithEvents<CustomerProductModel> repository)
        {
            _repository = repository;
        }

        public async Task<List<CustomerProductModel>> Handle(GetAllCustomerProductReques request, CancellationToken cancellationToken)
        {
            List<CustomerProductModel> returnValue = new List<CustomerProductModel>();
            returnValue = await _repository.ListAsync(cancellationToken);
            return returnValue;
        }
    }
}
