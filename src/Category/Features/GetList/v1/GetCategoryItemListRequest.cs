using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSH.Framework.Core.Paging;
using MediatR;

namespace Category.Features.GetList.v1;
 
public record GetCategoryItemListRequest(PaginationFilter Filter) : IRequest<PagedList<CategoryItemDto>>;
