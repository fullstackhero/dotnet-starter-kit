using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.CommonModel.DocumentData.Document;
public class DocumentDto : IDto
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
}
