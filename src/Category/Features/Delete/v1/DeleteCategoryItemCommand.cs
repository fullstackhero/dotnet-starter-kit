using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Category.Features.Delete.v1;
 
public sealed record DeleteCategoryItemCommand(
    Guid Id) : IRequest;



