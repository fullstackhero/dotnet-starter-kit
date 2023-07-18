using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Calls;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.CommonModel.DocumentData.DocumentType;
public class GetAllDocumentTypeRequest : IRequest<List<DocumentTypeModel>>
{
    public GetAllDocumentTypeRequest()
    {
            
    }

    public class GetAllDocumentTypeRequestHandler : IRequestHandler<GetAllDocumentTypeRequest, List<DocumentTypeModel>>
    {
        private readonly IRepositoryWithEvents<DocumentTypeModel> _repository;

        public GetAllDocumentTypeRequestHandler(IRepositoryWithEvents<DocumentTypeModel> repository)
        {
            _repository = repository;
        }

        public async Task<List<DocumentTypeModel>> Handle(GetAllDocumentTypeRequest request, CancellationToken cancellationToken)
        {
            List<DocumentTypeModel> documentTypes = new List<DocumentTypeModel>();
            documentTypes = await _repository.ListAsync(cancellationToken);
            return documentTypes;
        }
    }
}
