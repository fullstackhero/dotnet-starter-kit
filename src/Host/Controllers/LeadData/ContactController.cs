using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Application.LeadData.Contact;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using Microsoft.AspNetCore.Mvc;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.LeadData;
public class ContactController : VersionedApiController
{
    [HttpPost]
    //[MustHavePermission(FLRetailERPAction.Create, FLRetailERPResource.Suppliers)]
    [OpenApiOperation("Create a new Contact.", "")]
    public Task<Guid> CreateAsync(CreateContactRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpGet("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.View, FLRetailERPResource.Customers)]
    [OpenApiOperation("Get Contact details By Id.", "")]
    public Task<ContactDto> GetAsync(Guid id)
    {
        return Mediator.Send(new GetContactRequestById(id));
    }

    [HttpGet]
    [OpenApiOperation("Get All Contact details.", "")]
    public async Task<List<ContactDetailsModel>> GetListAsync()
    {
        List<ContactDetailsModel> contacts = new();
        contacts = await Mediator.Send(new GetAllContactRequest());
        return contacts;
    }

    [HttpPut("{id:guid}")]
    // [MustHavePermission(FLRetailERPAction.Update, FLRetailERPResource.Customers)]
    [OpenApiOperation("Update a Contact.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdateContactRequest request, Guid id)
    {
        return id != request.Id
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }

    [HttpDelete("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.Delete, FLRetailERPResource.Customers)]
    [OpenApiOperation("Delete a Contact By Id.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeleteContactRequestById(id));
    }

}
