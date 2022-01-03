using Ardalis.ApiEndpoints;
using DN.WebApi.Application.Common.Exceptions;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Catalog;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Domain.Dashboard;
using DN.WebApi.Host.Controllers;
using DN.WebApi.Infrastructure.Identity.Permissions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace DN.WebApi.Host.Endpoints.Catalog.Brands;

[ApiConventionType(typeof(FSHApiConventions))]
public class Create : EndpointBaseAsync
    .WithRequest<CreateBrandRequest>
    .WithResult<Result<Guid>>
{
    private readonly IRepositoryAsync _repository;
    private readonly IStringLocalizer<Create> _localizer;

    public Create(IRepositoryAsync repository, IStringLocalizer<Create> localizer)
    {
        _repository = repository;
        _localizer = localizer;
    }

    [HttpPost("")]
    [MustHavePermission(PermissionConstants.Brands.Register)]
    public override async Task<Result<Guid>> HandleAsync(CreateBrandRequest request, CancellationToken cancellationToken = default)
    {
        bool brandExists = await _repository.ExistsAsync<Brand>(a => a.Name == request.Name);
        if (brandExists) throw new EntityAlreadyExistsException(string.Format(_localizer["brand.alreadyexists"], request.Name));
        var brand = new Brand(request.Name, request.Description);
        brand.DomainEvents.Add(new StatsChangedEvent());
        var brandId = await _repository.CreateAsync(brand);
        await _repository.SaveChangesAsync();
        return await Result<Guid>.SuccessAsync(brandId);
    }
}