using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Lead;
public class GetLeadRequestById : IRequest<LeadDto>
{
    public Guid Id { get; set; }

    public GetLeadRequestById(Guid id) => Id = id;
}

public class LeadByIdSpec : Specification<LeadDetailsModel, LeadDto>, ISingleResultSpecification
{
    public LeadByIdSpec(Guid id) =>
        Query.Where(p => p.Id == id);
}

public class GetLeadRequestHandler : IRequestHandler<GetLeadRequestById, LeadDto>
{
    private readonly IRepository<LeadDetailsModel> _repository;
    private readonly IStringLocalizer _t;

    public GetLeadRequestHandler(IRepository<LeadDetailsModel> repository, IStringLocalizer<GetLeadRequestHandler> localizer) => (_repository, _t) = (repository, localizer);

    public async Task<LeadDto> Handle(GetLeadRequestById request, CancellationToken cancellationToken) =>
        await _repository.GetBySpecAsync(
            (ISpecification<LeadDetailsModel, LeadDto>)new LeadByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["Lead {0} Not Found.", request.Id]);
}