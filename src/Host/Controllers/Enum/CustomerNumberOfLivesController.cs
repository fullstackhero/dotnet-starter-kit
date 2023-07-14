using FL_CRMS_ERP_WEBAPI.Domain.Enum;
using FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.CustomerCompanyName;
using FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.CustomerNumberOfLives;
using Microsoft.AspNetCore.Mvc;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.Enum;

public class CustomerNumberOfLivesController : VersionedApiController
{
    [HttpGet]
    [OpenApiOperation("Get All Customer Number Of Lives.", "")]
    public async Task<List<CustomerNumberOfLivesModel>> GetListAsync()
    {
        return (await Mediator.Send(new GetAllCustomerNumberOfLivesReques()));
    }

    [HttpPost]
    [OpenApiOperation("Create a new Customer Number Of Lives.", "")]
    public Task<Guid> CreateAsync(CreateCustomerNumberOfLives request)
    {
        return Mediator.Send(request);
    }
}
