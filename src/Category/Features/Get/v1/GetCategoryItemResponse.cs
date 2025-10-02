using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Category.Features.Get.v1;
 
public record GetCategoryItemResponse(Guid? Id, string? Name, string? Description);
