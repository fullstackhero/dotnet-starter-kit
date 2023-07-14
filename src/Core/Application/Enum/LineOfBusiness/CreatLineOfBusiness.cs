using FL_CRMS_ERP_WEBAPI.Domain.Enum;
using FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.InvoiceStatus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.LineOfBusiness;
public class CreatLineOfBusiness : IRequest<DefaultIdType>
{
    public string? BusinessName { get; set; }
}

public class CreatLineOfBusinessHandler : IRequestHandler<CreatLineOfBusiness, DefaultIdType>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<LineOfBusinessModel> _repository;

    public CreatLineOfBusinessHandler(IRepositoryWithEvents<LineOfBusinessModel> repository) => _repository = repository;

    public async Task<DefaultIdType> Handle(CreatLineOfBusiness request, CancellationToken cancellationToken)
    {
        var invoice = new LineOfBusinessModel(request.BusinessName);

        await _repository.AddAsync(invoice, cancellationToken);

        return invoice.Id;
    }
}
