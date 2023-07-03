using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Calls;
using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Meeting;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.CommonModel.DocumentData.DocumentType;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.CommonModel;

public class DocumentTypeController : VersionedApiController
{
    [HttpPost]
    //[MustHavePermission(FLRetailERPAction.Create, FLRetailERPResource.Suppliers)]
    [OpenApiOperation("Create a new DocumentType.", "")]
    public Task<Guid> CreateAsync(CreateDocumentTypeRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpPut("{id:guid}")]
    // [MustHavePermission(FLRetailERPAction.Update, FLRetailERPResource.Customers)]
    [OpenApiOperation("Update a Document Type.", "")]
    public async Task<ActionResult<Guid>> UpdateAsync(UpdateDocumentTypeRequest request, Guid id)
    {
        return id != request.Id
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }

    [HttpDelete("{id:guid}")]
    //[MustHavePermission(FLRetailERPAction.Delete, FLRetailERPResource.Customers)]
    [OpenApiOperation("Delete a Document Type By Id.", "")]
    public Task<Guid> DeleteAsync(Guid id)
    {
        return Mediator.Send(new DeleteDocumentTypeRequestById(id));
    }

    [HttpGet]
    [OpenApiOperation("Get All Document Type details.", "")]
    public async Task<List<DocumentTypeModel>> GetListAsync()
    {
        List<DocumentTypeModel> documentTypes = new();
        documentTypes = await Mediator.Send(new GetAllDocumentTypeRequest());
        return documentTypes;
    }
}
