using FL_CRMS_ERP_WEBAPI.Application.LeadData.Lead;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.CommonModel.Notes;
public class CreateNotesRequest : IRequest<DefaultIdType>
{
    public Guid NoteOwnerId { get; set; }
    public string? NoteTitle { get; set; }
    public string? NoteContent { get; set; }
    public Guid? ParentId { get; set; }
    public string? RelatedTo { get; set; }
}

public class CreateNotesRequestHandler : IRequestHandler<CreateNotesRequest, DefaultIdType>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<NotesModel> _repository;

    public CreateNotesRequestHandler(IRepositoryWithEvents<NotesModel> repository) => _repository = repository;

    public async Task<DefaultIdType> Handle(CreateNotesRequest request, CancellationToken cancellationToken)
    {
        var notes = new NotesModel(request.NoteOwnerId, request.NoteTitle, request.NoteContent, request.ParentId, request.RelatedTo);

        await _repository.AddAsync(notes, cancellationToken);

        return notes.Id;
    }
}
