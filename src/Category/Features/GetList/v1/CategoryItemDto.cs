using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Category.Features.GetList.v1;
 
public record CategoryItemDto(Guid? Id, string Name, string Description);
