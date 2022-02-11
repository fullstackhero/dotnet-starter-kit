using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Application.Catalog.Products;

public class ProductExportDto : IDto
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Rate { get; set; } = default!;
    public string BrandName { get; set; } = default!;
}
