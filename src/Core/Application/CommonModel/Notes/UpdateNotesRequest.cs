using FL_CRMS_ERP_WEBAPI.Application.LeadData.Lead;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.CommonModel.Notes;
public class UpdateNotesRequest : IRequest<Guid>
{
    public Guid Id { get; set; }
    public Guid NoteOwnerId { get; set; }
    public string? NoteTitle { get; set; }
    public string? NoteContent { get; set; }
    public Guid ParentId { get; set; }
    public string? RelatedTo { get; set; }

    public class UpdateNotesRequestHandler : IRequestHandler<UpdateNotesRequest, Guid>
    {
        // Add Domain Events automatically by using IRepositoryWithEvents
        private readonly IRepositoryWithEvents<NotesModel> _repository;
        private readonly IStringLocalizer _t;

        public UpdateNotesRequestHandler(IRepositoryWithEvents<NotesModel> repository, IStringLocalizer<UpdateNotesRequestHandler> localizer) =>
            (_repository, _t) = (repository, localizer);

        public async Task<Guid> Handle(UpdateNotesRequest request, CancellationToken cancellationToken)
        {
            var notes = await _repository.GetByIdAsync(request.Id, cancellationToken);

            _ = notes
            ?? throw new NotFoundException(_t["Note {0} Not Found.", request.Id]);

            notes.Update(request.NoteOwnerId, request?.NoteTitle, request?.NoteContent, request.ParentId, request?.RelatedTo);

            await _repository.UpdateAsync(notes, cancellationToken);

            return request.Id;
        }
    }
}
