using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Application.Catalog.Filters;
public class FilterDto: IDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    
    public string Color { get; set; }
}
