using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Notes;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.CommonModel.Calls;
public class GetCallsRequestById : IRequest<CallsDto>
{
    public Guid Id { get; set; }

    public GetCallsRequestById(Guid id) => Id = id;

}

public class CallByIdSpec : Specification<CallsModel, CallsDto>, ISingleResultSpecification
{
    public CallByIdSpec(Guid id) =>
        Query.Where(p => p.Id == id);
}


public class GetCallsRequestHandler : IRequestHandler<GetCallsRequestById, CallsDto>
{
    private readonly IRepository<CallsModel> _repository;
    private readonly IStringLocalizer _t;

    public GetCallsRequestHandler(IRepository<CallsModel> repository, IStringLocalizer<GetCallsRequestHandler> localizer) => (_repository, _t) = (repository, localizer);

    public async Task<CallsDto> Handle(GetCallsRequestById request, CancellationToken cancellationToken) =>
        await _repository.GetBySpecAsync(
            (ISpecification<CallsModel, CallsDto>)new CallByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["Call {0} Not Found.", request.Id]);
}
