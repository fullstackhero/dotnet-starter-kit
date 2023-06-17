using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Application.LeadData.Contact;
using FL_CRMS_ERP_WEBAPI.Application.LeadData.Customer;
using FL_CRMS_ERP_WEBAPI.Application.LeadData.Lead;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.LeadData;

public class CustomerController : VersionedApiController
{
    [HttpPost]
    //[MustHavePermission(FLRetailERPAction.Create, FLRetailERPResource.Suppliers)]
    [OpenApiOperation("Create a new Customer.", "")]
    public Task<Guid> CreateAsync(CreateCustomerRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpGet]
    [OpenApiOperation("Get All Customer details.", "")]
    public async Task<List<CustomerDetailsModel>> GetListAsync()
    {
        List<CustomerDetailsModel> customers = new();
        customers = await Mediator.Send(new GetAllCustomerRequest());
        return customers;
    }

    [HttpGet("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.View, FLRetailERPResource.Customers)]
    [OpenApiOperation("Get Customer details By Id.", "")]
    public Task<CustomerDto> GetAsync(Guid id)
    {
        return Mediator.Send(new GetCustomerRequestById(id));
    }

    [HttpPut("{id:guid}")]
    // [MustHavePermission(FLRetailERPAction.Update, FLRetailERPResource.Customers)]
    [OpenApiOperation("Update a Customer.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdateCustomerRequest request, Guid id)
    {
        return id != request.Id
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }

    [HttpDelete("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.Delete, FLRetailERPResource.Customers)]
    [OpenApiOperation("Delete a Customer By Id.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeleteCustomerRequestById(id));
    }
}
