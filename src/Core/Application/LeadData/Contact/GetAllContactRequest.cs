using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Contact;
public class GetAllContactRequest : IRequest<List<ContactDetailsModel>>
{
    public GetAllContactRequest()
    {

    }

    public class GetAllContactRequestHandler : IRequestHandler<GetAllContactRequest, List<ContactDetailsModel>>
    {
        private readonly IRepositoryWithEvents<ContactDetailsModel> _repository;

        public GetAllContactRequestHandler(IRepositoryWithEvents<ContactDetailsModel> repository)
        {
            _repository = repository;
        }

        public async Task<List<ContactDetailsModel>> Handle(GetAllContactRequest request, CancellationToken cancellationToken)
        {
            List<ContactDetailsModel> contacts = new List<ContactDetailsModel>();
            contacts = await _repository.ListAsync(cancellationToken);
            return contacts;
        }
    }
}
