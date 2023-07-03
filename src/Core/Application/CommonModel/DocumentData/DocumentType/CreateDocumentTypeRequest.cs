using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.CommonModel.DocumentData.DocumentType;
public class CreateDocumentTypeRequest : IRequest<DefaultIdType>
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class CreateDocumentTypeRequestHandler : IRequestHandler<CreateDocumentTypeRequest, DefaultIdType>
{
    private readonly IRepositoryWithEvents<DocumentTypeModel> _repository;

    public CreateDocumentTypeRequestHandler(IRepositoryWithEvents<DocumentTypeModel> repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateDocumentTypeRequest request, CancellationToken cancellationToken)
    {
        var document = new DocumentTypeModel(request.Name, request.Description);
        await _repository.AddAsync(document);
        return document.Id;
    }
}
