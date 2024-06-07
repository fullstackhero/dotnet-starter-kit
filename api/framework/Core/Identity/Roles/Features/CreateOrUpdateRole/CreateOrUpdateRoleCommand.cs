using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.Framework.Core.Identity.Roles.Features.CreateOrUpdateRole;

public class CreateOrUpdateRoleCommand
{
    public string Name { get; set; }
    public string? Description { get; set; }
}
