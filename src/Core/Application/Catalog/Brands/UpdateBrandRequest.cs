using DN.WebApi.Application.Common.Exceptions;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Catalog;
using DN.WebApi.Domain.Dashboard;
using MediatR;
using Microsoft.Extensions.Localization;

namespace DN.WebApi.Application.Catalog.Brands;

public class UpdateBrandRequest : IRequest<Result<Guid>>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}

public class UpdateBrandRequestHandler : IRequestHandler<UpdateBrandRequest, Result<Guid>>
{
    private readonly IRepositoryAsync _repository;
    private readonly IStringLocalizer<UpdateBrandRequestHandler> _localizer;

    public UpdateBrandRequestHandler(IRepositoryAsync repository, IStringLocalizer<UpdateBrandRequestHandler> localizer) =>
        (_repository, _localizer) = (repository, localizer);

    public async Task<Result<Guid>> Handle(UpdateBrandRequest request, CancellationToken cancellationToken)
    {
        var brand = await _repository.GetByIdAsync<Brand>(request.Id, cancellationToken: cancellationToken);
        if (brand is null)
        {
            throw new EntityNotFoundException(string.Format(_localizer["brand.notfound"], request.Id));
        }

        var updatedBrand = brand.Update(request.Name, request.Description);
        updatedBrand.DomainEvents.Add(new StatsChangedEvent());
        await _repository.UpdateAsync(updatedBrand, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(request.Id);
    }
}