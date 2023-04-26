namespace FSH.WebApi.Application.Geo.Countries;

public class CountryDto : IDto
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
    public string? Iso2 { get; set; }
    public string? Iso3 { get; set; }

    public DefaultIdType ContinentId { get; set; }
    public string ContinentName { get; set; } = default!;
    public DefaultIdType? SubContinentId { get; set; }
    public string? SubContinentName { get; set; }

    public DefaultIdType TypeId { get; set; }
    public string TypeName { get; set; } = default!;
    public DefaultIdType? SubTypeId { get; set; }
    public string? SubTypeName { get; set; }

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

}

public class CountryDetailsDto : IDto
{
    public DefaultIdType Id { get; set; }

    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? NativeName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class CountryExportDto : IDto
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
    public string? Iso2 { get; set; }
    public string? Iso3 { get; set; }

    public DefaultIdType ContinentId { get; set; }
    public string ContinentName { get; set; } = default!;
    public DefaultIdType? SubContinentId { get; set; }
    public string? SubContinentName { get; set; }

    public DefaultIdType TypeId { get; set; }
    public string TypeName { get; set; } = default!;
    public DefaultIdType? SubTypeId { get; set; }
    public string? SubTypeName { get; set; }

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

    public DefaultIdType CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public DefaultIdType LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}