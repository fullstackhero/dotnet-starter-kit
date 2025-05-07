using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Category.Features.Update.v1;
 
public sealed record UpdateCategoryItemCommand(
    Guid Id,
    string? Name,
    string? Description = null) : IRequest<UpdateCategoryItemResponse>;



