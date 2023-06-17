using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Application.LeadData.Lead;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using Microsoft.AspNetCore.Mvc;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.LeadData;
public class AccountController : VersionedApiController
{
    [HttpPost]
    //[MustHavePermission(FLRetailERPAction.Create, FLRetailERPResource.Suppliers)]
    [OpenApiOperation("Create a new Account.", "")]
    public Task<Guid> CreateAsync(CreateAccountRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpGet]
    [OpenApiOperation("Get All Account details.", "")]
    public async Task<List<AccountDetailsModel>> GetListAsync()
    {
        List<AccountDetailsModel> accounts = new();
        accounts = await Mediator.Send(new GetAllAccountRequest());
        return accounts;
    }

    [HttpGet("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.View, FLRetailERPResource.Customers)]
    [OpenApiOperation("Get Account details By Id.", "")]
    public Task<AccountDto> GetAsync(Guid id)
    {
        return Mediator.Send(new GetAccountRequestById(id));
    }

    [HttpPut("{id:guid}")]
    // [MustHavePermission(FLRetailERPAction.Update, FLRetailERPResource.Customers)]
    [OpenApiOperation("Update a Account.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdateAccountRequest request, Guid id)
    {
        return id != request.Id
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }

    [HttpDelete("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.Delete, FLRetailERPResource.Customers)]
    [OpenApiOperation("Delete a Account By Id.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeleteAccountRequestById(id));
    }
}
