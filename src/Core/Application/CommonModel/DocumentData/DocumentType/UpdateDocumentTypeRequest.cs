using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Calls;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FL_CRMS_ERP_WEBAPI.Application.CommonModel.Calls.UpdateCallsRequest;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.CommonModel.DocumentData.DocumentType;
public class UpdateDocumentTypeRequest : IRequest<Guid>
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }

    public class UpdateDocumentTypeRequestHandler : IRequestHandler<UpdateDocumentTypeRequest, Guid>
    {
        private readonly IRepositoryWithEvents<DocumentTypeModel> _repository;
        private readonly IStringLocalizer _t;

        public UpdateDocumentTypeRequestHandler(IRepositoryWithEvents<DocumentTypeModel> repository, IStringLocalizer<UpdateCallsRequestHandler> localizer) =>
            (_repository, _t) = (repository, localizer);
        public async Task<Guid> Handle(UpdateDocumentTypeRequest request, CancellationToken cancellationToken)
        {
            var document = await _repository.GetByIdAsync(request.Id, cancellationToken);

            _ = document
            ?? throw new NotFoundException(_t["Document {0} Not Found.", request.Id]);

            document.Update(request.Name, request.Description);

            await _repository.UpdateAsync(document, cancellationToken);

            return request.Id;
        }
    }
}
