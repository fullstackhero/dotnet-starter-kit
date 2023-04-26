using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Wards;

public class UpdateWardRequest : IRequest<DefaultIdType>
{
    public DefaultIdType Id { get; set; }

    public int Order { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    public string? FullName { get; set; }
    public string? NativeName { get; set; }
    public string? FullNativeName { get; set; }
    public int? NumericCode { get; set; }

    public string? Latitude { get; set; }
    public string? Longitude { get; set; }

    public DefaultIdType TypeId { get; set; }
    public DefaultIdType DistrictId { get; set; }
}

public class UpdateWardRequestHandler : IRequestHandler<UpdateWardRequest, DefaultIdType>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<Ward> _repository;
    private readonly IStringLocalizer _t;
    private readonly IFileStorageService _file;

    public UpdateWardRequestHandler(IRepositoryWithEvents<Ward> repository, IStringLocalizer<UpdateWardRequestHandler> localizer, IFileStorageService file) =>
       (_repository, _t, _file) = (repository, localizer, file);

    public async Task<DefaultIdType> Handle(UpdateWardRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = entity
        ?? throw new NotFoundException(_t["Entity {0} Not Found.", request.Id]);

        entity.Update(
            request.Order,
            request.Code,
            request.Name,
            request.Description,
            request.IsActive,
            request.FullName,
            request.NativeName,
            request.FullNativeName,
            request.NumericCode,
            request.Latitude,
            request.Longitude,
            request.TypeId,
            request.DistrictId);

        await _repository.UpdateAsync(entity, cancellationToken);

        return request.Id;
    }
}