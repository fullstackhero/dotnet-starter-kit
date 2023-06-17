using FL_CRMS_ERP_WEBAPI.Application.LeadData.Lead;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.CommonModel.Notes;
public class GetAllNotesRequest : IRequest<List<NotesModel>>
{
    public GetAllNotesRequest()
    {

    }
    public class GetAllNotesRequestHandler : IRequestHandler<GetAllNotesRequest, List<NotesModel>>
    {
        private readonly IRepositoryWithEvents<NotesModel> _repository;

        public GetAllNotesRequestHandler(IRepositoryWithEvents<NotesModel> repository)
        {
            _repository = repository;
        }

        public async Task<List<NotesModel>> Handle(GetAllNotesRequest request, CancellationToken cancellationToken)
        {
            List<NotesModel> notes = new List<NotesModel>();
            notes = await _repository.ListAsync(cancellationToken);


            return notes;
        }
    }
}
