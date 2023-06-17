using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Application.LeadData.Quotation;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.LeadData;

public class QuotationController : VersionedApiController
{
    [HttpPost]
    //[MustHavePermission(FLRetailERPAction.Create, FLRetailERPResource.Suppliers)]
    [OpenApiOperation("Create a new Quotation.", "")]
    public Task<Guid> CreateAsync(CreateQuotationRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpGet]
    [OpenApiOperation("Get All Quotation details.", "")]
    public async Task<List<QuotationDetailsModel>> GetListAsync()
    {
        List<QuotationDetailsModel> quotations = new();
        quotations = await Mediator.Send(new GetAllQuotationRequest());
        return quotations;
    }

    [HttpGet("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.View, FLRetailERPResource.Customers)]
    [OpenApiOperation("Get Quotation details By Id.", "")]
    public Task<QuotationDto> GetAsync(Guid id)
    {
        return Mediator.Send(new GetQuotationRequestById(id));
    }

    [HttpPut("{id:guid}")]
    // [MustHavePermission(FLRetailERPAction.Update, FLRetailERPResource.Customers)]
    [OpenApiOperation("Update a Quotation.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdateQuotationRequest request, Guid id)
    {
        return id != request.Id
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }

    [HttpDelete("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.Delete, FLRetailERPResource.Customers)]
    [OpenApiOperation("Delete a Quotation By Id.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeleteQuotationRequestById(id));
    }
}
