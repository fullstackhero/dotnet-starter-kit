using FL_CRMS_ERP_WEBAPI.Domain.Enum;
using FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.CustomerPolicyStatus;
using Microsoft.AspNetCore.Mvc;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.Enum;

public class CustomerPolicyStatusController : VersionedApiController
{
    [HttpGet]
    [OpenApiOperation("Get All Customer Policy Status.", "")]
    public async Task<List<CustomerPolicyStatusModel>> GetListAsync()
    {
        return (await Mediator.Send(new GetAllCustomerPolicyStatusReques()));
    }

    [HttpPost]
    [OpenApiOperation("Create a new Customer Policy Status.", "")]
    public Task<Guid> CreateAsync(CreateCustomerPolicyStatus request)
    {
        return Mediator.Send(request);
    }
}
