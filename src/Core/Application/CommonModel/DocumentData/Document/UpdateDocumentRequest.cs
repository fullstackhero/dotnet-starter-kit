using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Calls;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.CommonModel.DocumentData.Document;
public class UpdateDocumentRequest : IRequest<Guid>
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = false;
    public string? URL { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public virtual DocumentTypeModel? DocumentType { get; set; }
    public Guid? DocumentOwnerID { get; set; }
    public Guid? ParentID { get; set; }
    public string? RelatedTo { get; set; }

    public class UpdateDocumentRequestHandler : IRequestHandler<UpdateDocumentRequest, Guid>
    {
        // Add Domain Events automatically by using IRepositoryWithEvents
        private readonly IRepositoryWithEvents<DocumentModel> _repository;
        private readonly IStringLocalizer _t;

        public UpdateDocumentRequestHandler(IRepositoryWithEvents<DocumentModel> repository, IStringLocalizer<UpdateDocumentRequestHandler> localizer) =>
            (_repository, _t) = (repository, localizer);

        public async Task<Guid> Handle(UpdateDocumentRequest request, CancellationToken cancellationToken)
        {
            var document = await _repository.GetByIdAsync(request.Id, cancellationToken);

            _ = document
            ?? throw new NotFoundException(_t["Document {0} Not Found.", request.Id]);

            document.Update(request.Title, request.Description, request.IsPublic, request.URL, request.DocumentTypeId, request.DocumentType, request.DocumentOwnerID, request.ParentID, request.RelatedTo);

            await _repository.UpdateAsync(document, cancellationToken);

            return request.Id;
        }
    }
}
