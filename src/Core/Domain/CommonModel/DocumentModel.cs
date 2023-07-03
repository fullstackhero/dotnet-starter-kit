using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
public class DocumentModel : AuditableEntity, IAggregateRoot
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = false;
    public string? URL { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public virtual DocumentTypeModel? DocumentType { get; set; }
    public Guid? DocumentOwnerID { get; set; }
    public Guid? ParentID { get; set; }
    public string? RelatedTo { get; set; }
    //public DocumentModel(string? title, string? description, bool isPublic, string? uRL, Guid? documentTypeId, DocumentTypeModel? dOCUMENTType, Guid? documentOwnerID, Guid? parentID, string? relatedTo)
    //{
    //    Title = title;
    //    Description = description;
    //    IsPublic = isPublic;
    //    URL = uRL;
    //    DocumentTypeId = documentTypeId;
    //    DocumentType = dOCUMENTType;
    //    DocumentOwnerID = documentOwnerID;
    //    ParentID = parentID;
    //    RelatedTo = relatedTo;
    //}

    // Default constructor required by Entity Framework
    protected DocumentModel()
    {
    }

    public DocumentModel(string? title, string? description, bool isPublic, string? url, Guid? documentTypeId, DocumentTypeModel? documentType, Guid? documentOwnerID, Guid? parentID, string? relatedTo)
    {
        Title = title;
        Description = description;
        IsPublic = isPublic;
        URL = url;
        DocumentTypeId = documentTypeId;
        DocumentType = documentType;
        DocumentOwnerID = documentOwnerID;
        ParentID = parentID;
        RelatedTo = relatedTo;
    }
    public DocumentModel Update(string? title, string? description, bool isPublic, string? url, Guid? documentTypeId, DocumentTypeModel? documentType, Guid? documentOwnerID, Guid? parentId, string? relatedTo)
    {
        if (title is not null && Title?.Equals(title) is not true) Title = title;
        if (description is not null && Description?.Equals(description) is not true) Description = description;
        if(IsPublic != isPublic) IsPublic = isPublic;
        if (url is not null && URL?.Equals(url) is not true) URL = url;
        if (documentTypeId != Guid.Empty && !DocumentTypeId.Equals(documentTypeId)) DocumentTypeId = documentTypeId;
        if(DocumentType != documentType) DocumentType = documentType;
        if (documentOwnerID != Guid.Empty && !DocumentOwnerID.Equals(documentOwnerID)) DocumentOwnerID = documentOwnerID;
        if (parentId != Guid.Empty && !ParentID.Equals(parentId)) ParentID = parentId;
        if (relatedTo is not null && RelatedTo?.Equals(relatedTo) is not true) RelatedTo = relatedTo;
        return this;
    }
}


