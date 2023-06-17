using FL_CRMS_ERP_WEBAPI.Application.LeadData.Lead;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.CommonModel.Notes;
public class GetNotesRequestById : IRequest<NotesDto>
{
    public Guid Id { get; set; }

    public GetNotesRequestById(Guid id) => Id = id;
}

public class NoteByIdSpec : Specification<NotesModel, NotesDto>, ISingleResultSpecification
{
    public NoteByIdSpec(Guid id) =>
        Query.Where(p => p.Id == id);
}

public class GetNotesRequestHandler : IRequestHandler<GetNotesRequestById, NotesDto>
{
    private readonly IRepository<NotesModel> _repository;
    private readonly IStringLocalizer _t;

    public GetNotesRequestHandler(IRepository<NotesModel> repository, IStringLocalizer<GetNotesRequestHandler> localizer) => (_repository, _t) = (repository, localizer);

    public async Task<NotesDto> Handle(GetNotesRequestById request, CancellationToken cancellationToken) =>
        await _repository.GetBySpecAsync(
            (ISpecification<NotesModel, NotesDto>)new NoteByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["Note {0} Not Found.", request.Id]);
}