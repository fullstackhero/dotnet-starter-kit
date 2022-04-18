using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Application.Catalog.Filters;
public class FilterByNameSpec: Specification<Filter>, ISingleResultSpecification
{
    public FilterByNameSpec(string name) => Query.Where(b => b.Name == name);
    
}
