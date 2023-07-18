using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Notes;
using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Task;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.CommonModel;

public class TaskController : VersionedApiController
{
    [HttpPost]
    //[MustHavePermission(FLRetailERPAction.Create, FLRetailERPResource.Suppliers)]
    [OpenApiOperation("Create a new Task.", "")]
    public Task<Guid> CreateAsync(CreateTaskRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpGet]
    [OpenApiOperation("Get All Task details.", "")]
    public async Task<List<TaskModel>> GetListAsync()
    {
        List<TaskModel> task = new();
        task = await Mediator.Send(new GetAllTaskRequest());
        return task;
    }

    [HttpDelete("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.Delete, FLRetailERPResource.Customers)]
    [OpenApiOperation("Delete a Task By Id.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeleteTaskRequestById(id));
    }

    [HttpPut("{id:guid}")]
    // [MustHavePermission(FLRetailERPAction.Update, FLRetailERPResource.Customers)]
    [OpenApiOperation("Update a Task.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdateTaskRequest request, Guid id)
    {
        return id != request.Id
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }

    [HttpGet("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.View, FLRetailERPResource.Customers)]
    [OpenApiOperation("Get Task details By Id.", "")]
    public Task<TaskDto> GetAsync(Guid id)
    {
        return Mediator.Send(new GetTaskRequestById(id));
    }
}
