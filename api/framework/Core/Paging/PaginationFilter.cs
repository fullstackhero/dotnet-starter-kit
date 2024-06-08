using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.Framework.Core.Paging;
public class PaginationFilter
{
    public int PageNumber { get; set; }

    public int PageSize { get; set; } = int.MaxValue;
}
