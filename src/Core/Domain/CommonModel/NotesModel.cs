using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
public class NotesModel : AuditableEntity, IAggregateRoot
{
    public Guid NoteOwnerId { get; set; }
    public string? NoteTitle { get; set; }
    public string? NoteContent { get; set; }
    public Guid ParentId { get; set; }
    public string? RelatedTo { get; set; }

    public NotesModel(Guid noteOwnerId, string? noteTitle, string? noteContent, Guid parentId, string? relatedTo)
    {
        NoteOwnerId = noteOwnerId;
        NoteTitle = noteTitle;
        NoteContent = noteContent;
        ParentId = parentId;
        RelatedTo = relatedTo;
    }

    public NotesModel Update(Guid noteOwnerId, string? noteTitle, string? noteContent, Guid parentId, string? relatedTo)
    {
        if (noteOwnerId != Guid.Empty && !NoteOwnerId.Equals(noteOwnerId)) NoteOwnerId = noteOwnerId;
        if (noteTitle is not null && NoteTitle?.Equals(noteTitle) is not true) NoteTitle = noteTitle;
        if (noteContent is not null && NoteContent?.Equals(noteContent) is not true) NoteContent = noteContent;
        if (parentId != Guid.Empty && !ParentId.Equals(parentId)) ParentId = parentId;
        if (relatedTo is not null && RelatedTo?.Equals(relatedTo) is not true) RelatedTo = relatedTo;
        return this;
    }
}
