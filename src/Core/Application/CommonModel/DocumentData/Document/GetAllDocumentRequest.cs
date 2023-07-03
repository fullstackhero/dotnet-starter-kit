using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Calls;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.CommonModel.DocumentData.Document;
public class GetAllDocumentRequest : IRequest<List<DocumentModel>>
{
    public GetAllDocumentRequest()
    {
            
    }
    public class GetAllDocumentRequestHandler : IRequestHandler<GetAllDocumentRequest, List<DocumentModel>>
    {
        private readonly IRepositoryWithEvents<DocumentModel> _repository;

        public GetAllDocumentRequestHandler(IRepositoryWithEvents<DocumentModel> repository)
        {
            _repository = repository;
        }

        public async Task<List<DocumentModel>> Handle(GetAllDocumentRequest request, CancellationToken cancellationToken)
        {
            List<DocumentModel> documents = new List<DocumentModel>();
            documents = await _repository.ListAsync(cancellationToken);
            return documents;
        }
    }
}
