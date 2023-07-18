using FL_CRMS_ERP_WEBAPI.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.CustomerNumberOfLives;

public class GetAllCustomerNumberOfLivesReques : IRequest<List<CustomerNumberOfLivesModel>>
{
    public GetAllCustomerNumberOfLivesReques()
    {
    }

    public class GetAllCustomerNumberOfLivesRequesHandler : IRequestHandler<GetAllCustomerNumberOfLivesReques, List<CustomerNumberOfLivesModel>>
    {
        private readonly IRepositoryWithEvents<CustomerNumberOfLivesModel> _repository;

        public GetAllCustomerNumberOfLivesRequesHandler(IRepositoryWithEvents<CustomerNumberOfLivesModel> repository)
        {
            _repository = repository;
        }

        public async Task<List<CustomerNumberOfLivesModel>> Handle(GetAllCustomerNumberOfLivesReques request, CancellationToken cancellationToken)
        {
            List<CustomerNumberOfLivesModel> returnValue = new List<CustomerNumberOfLivesModel>();
            returnValue = await _repository.ListAsync(cancellationToken);
            return returnValue;
        }
    }
}
