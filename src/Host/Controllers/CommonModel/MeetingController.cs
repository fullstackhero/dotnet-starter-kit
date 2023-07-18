using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Meeting;
using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Notes;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.CommonModel;

public class MeetingController : VersionedApiController
{

    [HttpPost]
    //[MustHavePermission(FLRetailERPAction.Create, FLRetailERPResource.Suppliers)]
    [OpenApiOperation("Create a new Meeting.", "")]
    public Task<Guid> CreateAsync(CreateMeetingRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpGet]
    [OpenApiOperation("Get All Meeting details.", "")]
    public async Task<List<MeetingModel>> GetListAsync()
    {
        List<MeetingModel> meetings = new();
        meetings = await Mediator.Send(new GetAllMeetingRequest());
        return meetings;
    }

    [HttpGet("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.View, FLRetailERPResource.Customers)]
    [OpenApiOperation("Get Meeting details By Id.", "")]
    public Task<MeetingDto> GetAsync(Guid id)
    {
        return Mediator.Send(new GetMeetingRequestById(id));
    }

    [HttpPut("{id:guid}")]
    // [MustHavePermission(FLRetailERPAction.Update, FLRetailERPResource.Customers)]
    [OpenApiOperation("Update a Meeting.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdateMeetingRequest request, Guid id)
    {
        return id != request.Id
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }

    [HttpDelete("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.Delete, FLRetailERPResource.Customers)]
    [OpenApiOperation("Delete a Meeting By Id.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeleteMeetingRequestById(id));
    }
}
