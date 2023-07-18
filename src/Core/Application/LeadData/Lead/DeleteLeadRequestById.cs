using FL_CRMS_ERP_WEBAPI.Application.Catalog.Products;
using FL_CRMS_ERP_WEBAPI.Domain.Common.Events;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Lead;
public class DeleteLeadRequestById : IRequest<Guid>
{
    public Guid Id { get; set; }

    public DeleteLeadRequestById(Guid id) => Id = id;
}

public class DeleteLeadRequestHandler : IRequestHandler<DeleteLeadRequestById, Guid>
{
    private readonly IRepository<LeadDetailsModel> _repository;
    private readonly IStringLocalizer _t;

    public DeleteLeadRequestHandler(IRepository<LeadDetailsModel> repository, IStringLocalizer<DeleteProductRequestHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<Guid> Handle(DeleteLeadRequestById request, CancellationToken cancellationToken)
    {
        var lead = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = lead ?? throw new NotFoundException(_t["Lead {0} Not Found."]);

        // Add Domain Events to be raised after the commit
        lead.DomainEvents.Add(EntityDeletedEvent.WithEntity(lead));

        await _repository.DeleteAsync(lead, cancellationToken);

        return request.Id;
    }
}
