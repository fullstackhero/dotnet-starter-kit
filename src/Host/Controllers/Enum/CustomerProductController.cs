using FL_CRMS_ERP_WEBAPI.Domain.Enum;
using FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.CustomerProduct;
using Microsoft.AspNetCore.Mvc;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.Enum;

public class CustomerProductController : VersionedApiController
{
    [HttpGet]
    [OpenApiOperation("Get All Customer Product Name details.", "")]
    public async Task<List<CustomerProductModel>> GetListAsync()
    {
        return (await Mediator.Send(new GetAllCustomerProductReques()));
    }

    [HttpPost]
    [OpenApiOperation("Create a new Customer Product Name.", "")]
    public Task<Guid> CreateAsync(CreateCustomerProduct request)
    {
        return Mediator.Send(request);
    }
}
