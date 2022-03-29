using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Application.Catalog.GameTypes;
public class GameTypeByNameSpec: Specification<GameType>, ISingleResultSpecification
{
    public GameTypeByNameSpec(string name) => Query.Where(b => b.Name == name);
    
}
