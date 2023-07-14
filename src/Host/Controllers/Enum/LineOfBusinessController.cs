using FL_CRMS_ERP_WEBAPI.Domain.Enum;
using FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.InvoiceStatus;
using FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.LineOfBusiness;
using Microsoft.AspNetCore.Mvc;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.Enum;
public class LineOfBusinessController : VersionedApiController
{
    [HttpGet]
    [OpenApiOperation("Get All Line of Business details.", "")]
    public async Task<List<LineOfBusinessModel>> GetListAsync()
    {
        List<LineOfBusinessModel> invoices = new();
        invoices = await Mediator.Send(new GetAllLineOfBusinessRequest());
        return invoices;
    }

    [HttpPost]
    [OpenApiOperation("Create a new InvoiceLine of Business.", "")]
    public Task<Guid> CreateAsync(CreatLineOfBusiness request)
    {
        return Mediator.Send(request);
    }
}
