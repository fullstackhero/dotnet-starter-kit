using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.Common.Events;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Customer;
public class DeleteCustomerRequestById : IRequest<Guid> 
{
    public Guid Id { get; set; }

    public DeleteCustomerRequestById(Guid id) => Id = id;
}

public class DeleteCustomerRequestHandler : IRequestHandler<DeleteCustomerRequestById, Guid>
{
    private readonly IRepository<CustomerDetailsModel> _repository;
    private readonly IStringLocalizer _t;

    public DeleteCustomerRequestHandler(IRepository<CustomerDetailsModel> repository, IStringLocalizer<DeleteCustomerRequestHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<Guid> Handle(DeleteCustomerRequestById request, CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = customer ?? throw new NotFoundException(_t["Customer {0} Not Found."]);

        // Add Domain Events to be raised after the commit
        customer.DomainEvents.Add(EntityDeletedEvent.WithEntity(customer));

        await _repository.DeleteAsync(customer, cancellationToken);

        return request.Id;
    }
}