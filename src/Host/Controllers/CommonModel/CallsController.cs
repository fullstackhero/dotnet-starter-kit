using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Calls;
using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Meeting;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.CommonModel;

public class CallsController : VersionedApiController
{
    [HttpPost]
    //[MustHavePermission(FLRetailERPAction.Create, FLRetailERPResource.Suppliers)]
    [OpenApiOperation("Create a new Calls.", "")]
    public Task<Guid> CreateAsync(CreateCallRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpGet]
    [OpenApiOperation("Get All Calls details.", "")]
    public async Task<List<CallsModel>> GetListAsync()
    {
        List<CallsModel> calls = new();
        calls = await Mediator.Send(new GetAllCallsRequest());
        return calls;
    }

    [HttpGet("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.View, FLRetailERPResource.Customers)]
    [OpenApiOperation("Get Calls details By Id.", "")]
    public Task<CallsDto> GetAsync(Guid id)
    {
        return Mediator.Send(new GetCallsRequestById(id));
    }

    [HttpDelete("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.Delete, FLRetailERPResource.Customers)]
    [OpenApiOperation("Delete a Calls By Id.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeleteCallRequestById(id));
    }

    [HttpPut("{id:guid}")]
    // [MustHavePermission(FLRetailERPAction.Update, FLRetailERPResource.Customers)]
    [OpenApiOperation("Update a Calls.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdateCallsRequest request, Guid id)
    {
        return id != request.Id
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }
}
