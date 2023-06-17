using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.Identity;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.Identity.Users.PersonalDetails;
public class GetPersonalRequestById : IRequest<PersonalDto>
{
    public Guid Id { get; set; }

    public GetPersonalRequestById(Guid id) => Id = id;
}

public class PersonalByIdSpec : Specification<PersonalDetailsModel, PersonalDto>, ISingleResultSpecification
{
    public PersonalByIdSpec(Guid id) =>
        Query.Where(p => p.Id == id);
}

public class GetPersonalRequestHandler : IRequestHandler<GetPersonalRequestById, PersonalDto>
{
    private readonly IRepository<PersonalDetailsModel> _repository;
    private readonly IStringLocalizer _t;

    public GetPersonalRequestHandler(IRepository<PersonalDetailsModel> repository, IStringLocalizer<GetPersonalRequestHandler> localizer) => (_repository, _t) = (repository, localizer);

    public async Task<PersonalDto> Handle(GetPersonalRequestById request, CancellationToken cancellationToken) =>
        await _repository.GetBySpecAsync(
            (ISpecification<PersonalDetailsModel, PersonalDto>)new PersonalByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["User {0} Not Found.", request.Id]);
}
