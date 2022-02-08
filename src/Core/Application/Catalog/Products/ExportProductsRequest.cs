using FSH.WebApi.Application.Common.Exporters;
using Mapster;

namespace FSH.WebApi.Application.Catalog.Products;

public class ExportProductsRequest : PaginationFilter, IRequest<Stream>
{
}

public class ExportProductsRequestHandler : IRequestHandler<ExportProductsRequest, Stream>
{
    private readonly IReadRepository<Product> _repository;
    private readonly IExcelWriter _excelWriter;

    public ExportProductsRequestHandler(IReadRepository<Product> repository, IExcelWriter excelWriter)
    {
        _repository = repository;
        _excelWriter = excelWriter;
    }

    public async Task<Stream> Handle(ExportProductsRequest request, CancellationToken cancellationToken)
    {
        var spec = new ExportProductsWithBrandsSpecification();

        var list = await _repository.ListAsync(spec, cancellationToken);

        return _excelWriter.WriteToStream(list);
    }
}

public class ExportProductsWithBrandsSpecification : Specification<Product, ProductExportDto>
{
    public ExportProductsWithBrandsSpecification()
    {
        Query
            .Include(p => p.Brand);
    }
}
