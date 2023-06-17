using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Application.LeadData.Invoice;
using FL_CRMS_ERP_WEBAPI.Application.LeadData.Lead;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.LeadData;
public class InvoiceController : VersionedApiController
{
    [HttpPost]
    //[MustHavePermission(FLRetailERPAction.Create, FLRetailERPResource.Suppliers)]
    [OpenApiOperation("Create a new Invoice.", "")]
    public Task<Guid> CreateAsync(CreateInvoiceRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpGet("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.View, FLRetailERPResource.Customers)]
    [OpenApiOperation("Get Invoice details By Id.", "")]
    public Task<InvoiceDto> GetAsync(Guid id)
    {
        return Mediator.Send(new GetInvoiceRequestById(id));
    }

    [HttpGet]
    [OpenApiOperation("Get All Invoice details.", "")]
    public async Task<List<InvoiceDetailsModel>> GetListAsync()
    {
        List<InvoiceDetailsModel> invoices = new();
        invoices = await Mediator.Send(new GetAllInvoiceRequest());
        return invoices;
    }

    [HttpPut("{id:guid}")]
    // [MustHavePermission(FLRetailERPAction.Update, FLRetailERPResource.Customers)]
    [OpenApiOperation("Update a Invoice.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdateInvoiceRequest request, Guid id)
    {
        return id != request.Id
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }

    [HttpDelete("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.Delete, FLRetailERPResource.Customers)]
    [OpenApiOperation("Delete a Invoice By Id.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeleteInvoiceRequestById(id));
    }
}
