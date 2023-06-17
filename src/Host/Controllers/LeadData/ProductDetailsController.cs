using FL_CRMS_ERP_WEBAPI.Application.Catalog.Products;
using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Application.LeadData.Contact;
using FL_CRMS_ERP_WEBAPI.Application.LeadData.Product;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.LeadData;

public class ProductDetailsController : VersionedApiController
{
    [HttpPost]
    //[MustHavePermission(FLRetailERPAction.Create, FLRetailERPResource.Suppliers)]
    [OpenApiOperation("Create a new Productdetails.", "")]
    public Task<Guid> CreateAsync(CreateProductRequestData request)
    {
        return Mediator.Send(request);
    }

    [HttpDelete("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.Delete, FLRetailERPResource.Customers)]
    [OpenApiOperation("Delete a Productdetails By Id.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeleteProductRequestById(id));
    }

    [HttpGet]
    [OpenApiOperation("Get All Productdetails.", "")]
    public async Task<List<ProductDetailsModel>> GetListAsync()
    {
        List<ProductDetailsModel> products = new();
        products = await Mediator.Send(new GetAllProductRequestData());
        return products;
    }

    [HttpPut("{id:guid}")]
    // [MustHavePermission(FLRetailERPAction.Update, FLRetailERPResource.Customers)]
    [OpenApiOperation("Update a Productdetails.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdateProductRequestData request, Guid id)
    {
        return id != request.Id
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }

    [HttpGet("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.View, FLRetailERPResource.Customers)]
    [OpenApiOperation("Get Productdetails By Id.", "")]
    public Task<ProductDtoData> GetAsync(Guid id)
    {
        return Mediator.Send(new GetProductRequestById(id));
    }
}
