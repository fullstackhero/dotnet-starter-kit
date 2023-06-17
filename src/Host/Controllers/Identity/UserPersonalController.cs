using FL_CRMS_ERP_WEBAPI.Application.Identity.Users.PersonalDetails;
using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Application.LeadData.Customer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.Identity;

public class UserPersonalController : VersionedApiController
{
    [HttpPost]
    //[MustHavePermission(FLRetailERPAction.Create, FLRetailERPResource.Suppliers)]
    [OpenApiOperation("Create a new User PersonalDetails.", "")]
    public Task<Guid> CreateAsync(CreatePersonalRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpDelete("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.Delete, FLRetailERPResource.Customers)]
    [OpenApiOperation("Delete a User PersonalDetails By Id.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeletePersonalRequestById(id));
    }

    [HttpPut("{id:guid}")]
    // [MustHavePermission(FLRetailERPAction.Update, FLRetailERPResource.Customers)]
    [OpenApiOperation("Update a PersonalDetails.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdatePersonalRequest request, Guid id)
    {
        return id != request.Id
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }

    [HttpGet("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.View, FLRetailERPResource.Customers)]
    [OpenApiOperation("Get User PersonalDetails By Id.", "")]
    public Task<PersonalDto> GetAsync(Guid id)
    {
        return Mediator.Send(new GetPersonalRequestById(id));
    }
}
