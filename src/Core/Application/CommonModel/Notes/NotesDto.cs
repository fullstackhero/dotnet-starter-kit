using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.CommonModel.Notes;
public class NotesDto
{
    public Guid Id { get; set; }
    public Guid NoteOwnerId { get; set; }
    public string? NoteTitle { get; set; }
    public string? NoteContent { get; set; }
    public Guid? ParentId { get; set; }
    public string? RelatedTo { get; set; }
}
