using FL_CRMS_ERP_WEBAPI.Application.LeadData.Lead;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
public class GetAccountRequestById : IRequest<AccountDto>
{
    public Guid Id { get; set; }

    public GetAccountRequestById(Guid id) => Id = id;
}

public class AccountByIdSpec : Specification<AccountDetailsModel, AccountDto>, ISingleResultSpecification
{
    public AccountByIdSpec(Guid id) =>
        Query.Where(p => p.Id == id);
}

public class GetAccountRequestHandler : IRequestHandler<GetAccountRequestById, AccountDto>
{
    private readonly IRepository<AccountDetailsModel> _repository;
    private readonly IStringLocalizer _t;

    public GetAccountRequestHandler(IRepository<AccountDetailsModel> repository, IStringLocalizer<GetAccountRequestHandler> localizer) => (_repository, _t) = (repository, localizer);

    public async Task<AccountDto> Handle(GetAccountRequestById request, CancellationToken cancellationToken) =>
        await _repository.GetBySpecAsync(
            (ISpecification<AccountDetailsModel, AccountDto>)new AccountByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["Account {0} Not Found.", request.Id]);
}
