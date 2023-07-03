using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Calls;
using FL_CRMS_ERP_WEBAPI.Domain.Common.Events;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.CommonModel.DocumentData.Document;
public class DeleteDocumentRequestById : IRequest<Guid>
{
    public Guid Id { get; set; }

    public DeleteDocumentRequestById(Guid id) => Id = id;
}

public class DeleteDocumentRequestByIdHandler : IRequestHandler<DeleteDocumentRequestById, Guid>
{
    private readonly IRepository<DocumentModel> _repository;
    private readonly IStringLocalizer _t;

    public DeleteDocumentRequestByIdHandler(IRepository<DocumentModel> repository, IStringLocalizer<DeleteCallRequestByIdHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<Guid> Handle(DeleteDocumentRequestById request, CancellationToken cancellationToken)
    {
        var document = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = document ?? throw new NotFoundException(_t["Call {0} Not Found."]);

        // Add Domain Events to be raised after the commit
        document.DomainEvents.Add(EntityDeletedEvent.WithEntity(document));

        await _repository.DeleteAsync(document, cancellationToken);

        return request.Id;
    }
}
