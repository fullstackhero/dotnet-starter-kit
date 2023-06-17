using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Contact;
public class GetContactRequestById : IRequest<ContactDto>
{
    public Guid Id { get; set; }

    public GetContactRequestById(Guid id) => Id = id;
}

public class ContactByIdSpec : Specification<ContactDetailsModel, ContactDto>, ISingleResultSpecification
{
    public ContactByIdSpec(Guid id) =>
        Query.Where(p => p.Id == id);
}

public class GetContactRequestHandler : IRequestHandler<GetContactRequestById, ContactDto>
{
    private readonly IRepository<ContactDetailsModel> _repository;
    private readonly IStringLocalizer _t;

    public GetContactRequestHandler(IRepository<ContactDetailsModel> repository, IStringLocalizer<GetAccountRequestHandler> localizer) => (_repository, _t) = (repository, localizer);

    public async Task<ContactDto> Handle(GetContactRequestById request, CancellationToken cancellationToken) =>
        await _repository.GetBySpecAsync(
            (ISpecification<ContactDetailsModel, ContactDto>)new ContactByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["Contact {0} Not Found.", request.Id]);
}
