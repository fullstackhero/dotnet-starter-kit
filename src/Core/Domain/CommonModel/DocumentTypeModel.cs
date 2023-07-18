using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
public class DocumentTypeModel : AuditableEntity, IAggregateRoot
{
    public string? Name { get; set; }
    public string? Description { get; set; }

    public DocumentTypeModel(string? name, string? description)
    {
        Name = name;
        Description = description;
    }

    public DocumentTypeModel Update(string? name, string? description)
    {
        if (name is not null && Name?.Equals(name) is not true) Name = name;
        if (description is not null && Description?.Equals(description) is not true) Description = description;
        return this;
    }
}
