using FL_CRMS_ERP_WEBAPI.Domain.Enum;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.InvoiceStatus;

public class CreatInvoiceStatus : IRequest<DefaultIdType>
{
    public string? StatusName { get; set; }
}

public class CreatInvoiceStatusHandler : IRequestHandler<CreatInvoiceStatus, DefaultIdType>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<InvoiceStatusModel> _repository;

    public CreatInvoiceStatusHandler(IRepositoryWithEvents<InvoiceStatusModel> repository) => _repository = repository;

    public async Task<DefaultIdType> Handle(CreatInvoiceStatus request, CancellationToken cancellationToken)
    {
        var invoice = new InvoiceStatusModel(request.StatusName);

        await _repository.AddAsync(invoice, cancellationToken);

        return invoice.Id;
    }
}
