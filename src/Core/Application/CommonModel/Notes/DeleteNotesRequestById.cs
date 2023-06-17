using FL_CRMS_ERP_WEBAPI.Application.Catalog.Products;
using FL_CRMS_ERP_WEBAPI.Application.LeadData.Lead;
using FL_CRMS_ERP_WEBAPI.Domain.Common.Events;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.CommonModel.Notes;
public class DeleteNotesRequestById : IRequest<Guid>
{
    public Guid Id { get; set; }

    public DeleteNotesRequestById(Guid id) => Id = id;
}

public class DeleteNotesRequestHandler : IRequestHandler<DeleteNotesRequestById, Guid>
{
    private readonly IRepository<NotesModel> _repository;
    private readonly IStringLocalizer _t;

    public DeleteNotesRequestHandler(IRepository<NotesModel> repository, IStringLocalizer<DeleteNotesRequestHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<Guid> Handle(DeleteNotesRequestById request, CancellationToken cancellationToken)
    {
        var notes = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = notes ?? throw new NotFoundException(_t["Note {0} Not Found."]);

        // Add Domain Events to be raised after the commit
        notes.DomainEvents.Add(EntityDeletedEvent.WithEntity(notes));

        await _repository.DeleteAsync(notes, cancellationToken);

        return request.Id;
    }
}