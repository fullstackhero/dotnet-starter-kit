using FL_CRMS_ERP_WEBAPI.Application.LeadData.Lead;
using FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.SendEmailNotification;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.EmailasPdf;

public class PdfGeneratorController : VersionedApiController
{
    [HttpPost]
    //[MustHavePermission(FLRetailERPAction.Create, FLRetailERPResource.Suppliers)]
    [OpenApiOperation("Create a new pdf.", "")]
    public Task<Unit> CreateAsync(CreatePdfRequest request)
    {
        return Mediator.Send(request);
    }
}
