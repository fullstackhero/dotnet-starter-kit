using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Product;
public class GetAllProductRequestData : IRequest<List<ProductDetailsModel>>
{
    public GetAllProductRequestData()
    {

    }

    public class GetAllProductRequestHandler : IRequestHandler<GetAllProductRequestData, List<ProductDetailsModel>>
    {
        private readonly IRepositoryWithEvents<ProductDetailsModel> _repository;

        public GetAllProductRequestHandler(IRepositoryWithEvents<ProductDetailsModel> repository)
        {
            _repository = repository;
        }

        public async Task<List<ProductDetailsModel>> Handle(GetAllProductRequestData request, CancellationToken cancellationToken)
        {
            List<ProductDetailsModel> products = new List<ProductDetailsModel>();
            products = await _repository.ListAsync(cancellationToken);


            return products;
        }
    }
}
