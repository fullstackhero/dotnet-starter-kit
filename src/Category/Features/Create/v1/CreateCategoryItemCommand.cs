using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Category.Features.Create.v1;
 
public record CreateCategoryItemCommand(
    [property: DefaultValue("Hello World!")] string Name,
    [property: DefaultValue("Important Description.")] string Description) : IRequest<CreateCategoryItemResponse>;


