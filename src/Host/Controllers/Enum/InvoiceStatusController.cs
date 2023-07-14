using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Application.LeadData.Invoice;
using FL_CRMS_ERP_WEBAPI.Domain.Enum;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.InvoiceStatus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.Enum;

public class InvoiceStatusController : VersionedApiController
{
    [HttpGet]
    [OpenApiOperation("Get All Invoice Status details.", "")]
    public async Task<List<InvoiceStatusModel>> GetListAsync()
    {
        List<InvoiceStatusModel> invoices = new();
        invoices = await Mediator.Send(new GetAllInvoiceStatusRequest());
        return invoices;
    }

    [HttpPost]
    //[MustHavePermission(FLRetailERPAction.Create, FLRetailERPResource.Suppliers)]
    [OpenApiOperation("Create a new Invoice status.", "")]
    public Task<Guid> CreateAsync(CreatInvoiceStatus request)
    {
        return Mediator.Send(request);
    }
}
