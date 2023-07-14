using FL_CRMS_ERP_WEBAPI.Domain.Enum;
using FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.CustomerCompanyName;
using FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.LineOfBusiness;
using Microsoft.AspNetCore.Mvc;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.Enum;

public class CustomerCompanyNameController : VersionedApiController
{
    [HttpGet]
    [OpenApiOperation("Get All Customer Company Name details.", "")]
    public async Task<List<CustomerCompanyNameModel>> GetListAsync()
    {
        return (await Mediator.Send(new GetAllCustomerCompanyNameReques())); 
    }

    [HttpPost]
    [OpenApiOperation("Create a new Customer Company Name.", "")]
    public Task<Guid> CreateAsync(CreateCustomerCompanyName request)
    {
        return Mediator.Send(request);
    }
}
