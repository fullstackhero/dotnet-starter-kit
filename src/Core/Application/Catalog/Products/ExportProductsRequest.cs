using FSH.WebApi.Application.Common.Exporters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Application.Catalog.Products;

public class ExportProductsRequest : PaginationFilter, IRequest<MemoryStream>
{
}

public class ExportProductsRequestHandler : IRequestHandler<ExportProductsRequest, MemoryStream>
{
    private readonly IReadRepository<Product> _repository;
    private readonly IExporter _exporter;

    public ExportProductsRequestHandler(IReadRepository<Product> repository, IExporter exporter)
    {
        _repository = repository;
        _exporter = exporter;
    }

    public async Task<MemoryStream> Handle(ExportProductsRequest request, CancellationToken cancellationToken)
    {
        var spec = new ExportProductsWithBrandsSpecification();

        var list = await _repository.ListAsync(spec, cancellationToken);

        var dt = _exporter.Convert(list);

        return _exporter.ExportToAsync(dt);
    }
}

public class ExportProductsWithBrandsSpecification : Specification<Product>
{
    public ExportProductsWithBrandsSpecification()
    {
        Query
            .Include(p => p.Brand);
    }
}
