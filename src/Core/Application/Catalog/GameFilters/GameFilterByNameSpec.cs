using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Application.Catalog.GameFilters;
public class GameFilterByNameSpec: Specification<GameFilter>, ISingleResultSpecification
{
    public GameFilterByNameSpec(string name) => Query.Where(b => b.Name == name);
    
}
