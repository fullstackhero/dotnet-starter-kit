using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Calls;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.CommonModel.DocumentData.Document;
public class GetDocumentRequestById : IRequest<DocumentDto>
{
    public Guid Id { get; set; }

    public GetDocumentRequestById(Guid id) => Id = id;
}

public class DocumentByIdSpec : Specification<DocumentModel, DocumentDto>, ISingleResultSpecification
{
    public DocumentByIdSpec(Guid id) =>
        Query.Where(p => p.Id == id);
}

public class GetCallsRequestHandler : IRequestHandler<GetDocumentRequestById, DocumentDto>
{
    private readonly IRepository<DocumentModel> _repository;
    private readonly IStringLocalizer _t;

    public GetCallsRequestHandler(IRepository<DocumentModel> repository, IStringLocalizer<GetCallsRequestHandler> localizer) => (_repository, _t) = (repository, localizer);

    public async Task<DocumentDto> Handle(GetDocumentRequestById request, CancellationToken cancellationToken) =>
        await _repository.GetBySpecAsync(
            (ISpecification<DocumentModel, DocumentDto>)new DocumentByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["Document {0} Not Found.", request.Id]);
}
