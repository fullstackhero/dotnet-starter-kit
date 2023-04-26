﻿using FSH.WebApi.Domain.Catalog;
using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Countries;

public class CreateCountryRequest : IRequest<DefaultIdType>
{
    public int Order { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    public string? FullName { get; set; }
    public string? NativeName { get; set; }
    public string? FullNativeName { get; set; }

    public int? NumericCode { get; set; }
    public string? Iso2 { get; set; }
    public string? Iso3 { get; set; }

    public DefaultIdType ContinentId { get; set; }
    public DefaultIdType? SubContinentId { get; set; }
    public DefaultIdType TypeId { get; set; }
    public DefaultIdType? SubTypeId { get; set; }

    public string? Capital { get; set; }
    public string? CurrencyCode { get; set; }
    public string? CurrencyName { get; set; }
    public string? CurrencySymbol { get; set; }

    public string? PhoneCode { get; set; }
    public string? InternetCode { get; set; }

    public string? Sovereignty { get; set; }
    public string? FlagPath { get; set; }
    public string? Emoji { get; set; }
    public string? EmojiU { get; set; }

    public string? Latitude { get; set; }
    public string? Longitude { get; set; }

    public FileUploadRequest? FlagImage { get; set; }
}

public class CreateCountryRequestHandler : IRequestHandler<CreateCountryRequest, DefaultIdType>
{
    private readonly IRepository<Country> _repository;
    private readonly IFileStorageService _file;

    public CreateCountryRequestHandler(IRepository<Country> repository, IFileStorageService file) =>
        (_repository, _file) = (repository, file);

    public async Task<DefaultIdType> Handle(CreateCountryRequest request, CancellationToken cancellationToken)
    {
        string flagPath = await _file.UploadAsync<Country>(request.FlagImage, FileType.Image, cancellationToken);

        var entity = new Country(
            request.Order,
            request.Code,
            request.Name,
            request.Description,
            request.IsActive,
            request.FullName,
            request.NativeName,
            request.FullNativeName,
            request.NumericCode,
            request.Iso2,
            request.Iso3,
            request.ContinentId,
            request.SubContinentId,
            request.TypeId,
            request.SubTypeId,
            request.Capital,
            request.CurrencyCode,
            request.CurrencyName,
            request.CurrencySymbol,
            request.PhoneCode,
            request.InternetCode,
            request.Sovereignty,
            flagPath,
            request.Emoji,
            request.EmojiU,
            request.Latitude,
            request.Longitude);

        // Add Domain Events to be raised after the commit
        entity.DomainEvents.Add(EntityCreatedEvent.WithEntity(entity));

        await _repository.AddAsync(entity, cancellationToken);

        return entity.Id;
    }
}
