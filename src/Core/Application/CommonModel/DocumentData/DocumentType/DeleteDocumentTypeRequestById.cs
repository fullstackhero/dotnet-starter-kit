using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Calls;
using FL_CRMS_ERP_WEBAPI.Domain.Common.Events;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.CommonModel.DocumentData.DocumentType;
public class DeleteDocumentTypeRequestById : IRequest<Guid>
{
    public Guid Id { get; set; }

    public DeleteDocumentTypeRequestById(Guid id) => Id = id;
}

public class DeleteDocumentTypeRequestByIdHandler : IRequestHandler<DeleteDocumentTypeRequestById, Guid>
{
    private readonly IRepository<DocumentTypeModel> _repository;
    private readonly IStringLocalizer _t;

    public DeleteDocumentTypeRequestByIdHandler(IRepository<DocumentTypeModel> repository, IStringLocalizer<DeleteDocumentTypeRequestByIdHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<Guid> Handle(DeleteDocumentTypeRequestById request, CancellationToken cancellationToken)
    {
        var documentType = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = documentType ?? throw new NotFoundException(_t["Document Type {0} Not Found."]);

        // Add Domain Events to be raised after the commit
        documentType.DomainEvents.Add(EntityDeletedEvent.WithEntity(documentType));

        await _repository.DeleteAsync(documentType, cancellationToken);

        return request.Id;
    }
}
