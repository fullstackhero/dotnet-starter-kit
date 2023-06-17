using DocumentFormat.OpenXml.Presentation;
using FL_CRMS_ERP_WEBAPI.Application.LeadData.Lead;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.LeadData;
//[Route("api/[controller]")]
//[ApiController]
public class LeadController : VersionedApiController
{
    [HttpPost]
    //[MustHavePermission(FLRetailERPAction.Create, FLRetailERPResource.Suppliers)]
    [OpenApiOperation("Create a new Lead.", "")]
    public Task<Guid> CreateAsync(CreateLeadRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpPut("{id:guid}")]
   // [MustHavePermission(FLRetailERPAction.Update, FLRetailERPResource.Customers)]
    [OpenApiOperation("Update a Lead.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdateLeadRequest request, Guid id)
    {
        return id != request.Id
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }

    [HttpGet]
    [OpenApiOperation("Get All Lead details.", "")]
    public async Task<List<LeadDetailsModel>> GetListAsync()
    {
        List<LeadDetailsModel> leads = new();
        leads = await Mediator.Send(new GetAllLeadRequest());
        return leads;
    }

    [HttpGet("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.View, FLRetailERPResource.Customers)]
    [OpenApiOperation("Get Lead details By Id.", "")]
    public Task<LeadDto> GetAsync(Guid id)
    {
        return Mediator.Send(new GetLeadRequestById(id));
    }

    [HttpDelete("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.Delete, FLRetailERPResource.Customers)]
    [OpenApiOperation("Delete a Lead By Id.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeleteLeadRequestById(id));
    }
}
