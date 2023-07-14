using FL_CRMS_ERP_WEBAPI.Domain.Enum;
using FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.CustomerCompanyName;
using FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.CustomerModeOfPayment;
using Microsoft.AspNetCore.Mvc;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.Enum;

public class CustomerModeOfPaymentController : VersionedApiController
{
    [HttpGet]
    [OpenApiOperation("Get All Customer Mode Of Payment.", "")]
    public async Task<List<CustomerModeOfPaymentModel>> GetListAsync()
    {
        return (await Mediator.Send(new GetAllCustomerModeOfPaymentReques()));
    }

    [HttpPost]
    [OpenApiOperation("Create a new Customer Mode Of Payment.", "")]
    public Task<Guid> CreateAsync(CreateCustomerModeOfPayment request)
    {
        return Mediator.Send(request);
    }
}
