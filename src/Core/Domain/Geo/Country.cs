namespace FSH.WebApi.Domain.Geo;
public class Country : AuditableEntity, IAggregateRoot
{
    public int Order { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; set; }

    public string? FullName { get; private set; }
    public string? NativeName { get; private set; }
    public string? FullNativeName { get; private set; }

    public int? NumericCode { get; private set; }
    public string? Iso2 { get; private set; }
    public string? Iso3 { get; private set; }

    public DefaultIdType ContinentId { get; private set; }
    public virtual GeoAdminUnit Continent { get; private set; } = default!;
    public DefaultIdType? SubContinentId { get; private set; }
    public virtual GeoAdminUnit? SubContinent { get; private set; }

    public DefaultIdType TypeId { get; private set; }
    public virtual GeoAdminUnit Type { get; private set; } = default!;
    public DefaultIdType? SubTypeId { get; private set; }
    public virtual GeoAdminUnit? SubType { get; private set; }

    public string? Capital { get; private set; }
    public string? CurrencyCode { get; private set; }
    public string? CurrencyName { get; private set; }
    public string? CurrencySymbol { get; private set; }

    public string? PhoneCode { get; private set; }
    public string? InternetCode { get; private set; }

    public string? Sovereignty { get; private set; }
    public string? FlagPath { get; private set; }
    public string? Emoji { get; set; }
    public string? EmojiU { get; set; }

    public string? Latitude { get; private set; } // Vĩ độ
    public string? Longitude { get; private set; } // Kinh độ

    public virtual ICollection<State> States { get; private set; } = default!;

    // public virtual ICollection<Province>? Province { get; set; }
    // public virtual ICollection<Timezone>? Timezones { get; set; }

    public Country(
        int order,
        string code,
        string name,
        string? description,
        bool isActive,
        string? fullName,
        string? nativeName,
        string? fullNativeName,
        int? numericCode,
        string? iso2,
        string? iso3,
        DefaultIdType continentId,
        DefaultIdType? subContinentId,
        DefaultIdType typeId,
        DefaultIdType? subTypeId,
        string? captital,
        string? currencyCode,
        string? currencyName,
        string? currencySymbol,
        string? phoneCode,
        string? internetCode,
        string? sovereignty,
        string? flagPath,
        string? emoji,
        string? emojiU,
        string? latitude,
        string? longitude )
    {
        Order = order;
        Code = code;
        Name = name;
        Description = description ?? string.Empty;
        IsActive = isActive;

        FullName = fullName;
        NativeName = nativeName;
        FullNativeName = fullNativeName;

        NumericCode = numericCode ?? 0;
        Iso2 = iso2;
        Iso3 = iso3;

        ContinentId = continentId;
        SubContinentId = (subContinentId == DefaultIdType.Empty) ? null : subContinentId;
        TypeId = typeId;
        SubTypeId = (subTypeId == DefaultIdType.Empty) ? null : subTypeId;

        Capital = captital;
        CurrencyCode = currencyCode;
        CurrencyName = currencyName;
        CurrencySymbol = currencySymbol;

        PhoneCode = phoneCode;
        InternetCode = internetCode;

        Sovereignty = sovereignty;
        FlagPath = flagPath;

        Emoji = emoji;
        EmojiU = emojiU;

        Latitude = latitude;
        Longitude = longitude;
    }

    public Country()
        : this(
            0,
            string.Empty,
            string.Empty,
            null,
            true,
            null,
            null,
            null,
            0,
            null,
            null,
            DefaultIdType.Empty,
            null,
            DefaultIdType.Empty,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null)
        {
        }

    public Country Update(
    int? order,
    string? code,
    string? name,
    string? description,
    bool? isActive,
    string? fullName,
    string? nativeName,
    string? fullNativeName,
    int? numericCode,
    string? iso2,
    string? iso3,
    DefaultIdType? continentId,
    DefaultIdType? subContinentId,
    DefaultIdType? typeId,
    DefaultIdType? subTypeId,
    string? captital,
    string? currencyCode,
    string? currencyName,
    string? currencySymbol,
    string? phoneCode,
    string? internetCode,
    string? sovereignty,
    string? flagPath,
    string? emoji,
    string? emojiU,
    string? latitude,
    string? longitude)
    {
        if (order is not null && order.HasValue && Order != order) Order = order.Value;
        if (code is not null && Code?.Equals(code) is not true) Code = code;
        if (name is not null && Name?.Equals(name) is not true) Name = name;
        if (description is not null && Description?.Equals(description) is not true) Description = description;
        if (isActive is not null && !IsActive.Equals(isActive)) IsActive = (bool)isActive;

        if (fullName is not null && FullName?.Equals(fullName) is not true) FullName = fullName;
        if (nativeName is not null && NativeName?.Equals(nativeName) is not true) NativeName = nativeName;
        if (fullNativeName is not null && FullNativeName?.Equals(fullNativeName) is not true) FullNativeName = fullNativeName;

        if (numericCode is not null && numericCode.HasValue && NumericCode != numericCode) NumericCode = numericCode.Value;
        if (iso2 is not null && Iso2?.Equals(iso2) is not true) Iso2 = iso2;
        if (iso3 is not null && Iso3?.Equals(iso3) is not true) Iso3 = iso3;

        if (continentId.HasValue && continentId.Value != DefaultIdType.Empty && !ContinentId.Equals(continentId.Value)) ContinentId = continentId.Value;
        if (subContinentId.HasValue && subContinentId.Value != DefaultIdType.Empty && !SubContinentId.Equals(subContinentId.Value)) SubContinentId = subContinentId.Value;
        if (typeId.HasValue && typeId.Value != DefaultIdType.Empty && !TypeId.Equals(typeId.Value)) TypeId = typeId.Value;
        if (subTypeId.HasValue && subTypeId.Value != DefaultIdType.Empty && !SubTypeId.Equals(subTypeId.Value)) SubTypeId = subTypeId.Value;

        if (captital is not null && Capital?.Equals(captital) is not true) Capital = captital;
        if (currencyCode is not null && CurrencyCode?.Equals(currencyCode) is not true) CurrencyCode = currencyCode;
        if (currencyName is not null && CurrencyName?.Equals(currencyCode) is not true) CurrencyName = currencyName;
        if (currencySymbol is not null && CurrencySymbol?.Equals(currencySymbol) is not true) CurrencySymbol = currencySymbol;

        if (phoneCode is not null && PhoneCode?.Equals(phoneCode) is not true) PhoneCode = phoneCode;
        if (internetCode is not null && InternetCode?.Equals(internetCode) is not true) InternetCode = internetCode;

        if (sovereignty is not null && Sovereignty?.Equals(sovereignty) is not true) Sovereignty = sovereignty;
        if (flagPath is not null && FlagPath?.Equals(flagPath) is not true) FlagPath = flagPath;
        if (emoji is not null && Emoji?.Equals(emoji) is not true) Emoji = emoji;
        if (emojiU is not null && EmojiU?.Equals(emojiU) is not true) EmojiU = emojiU;

        if (latitude is not null && Latitude?.Equals(latitude) is not true) Latitude = latitude;
        if (longitude is not null && Longitude?.Equals(longitude) is not true) Longitude = longitude;

        return this;
    }

    public Country ClearFlagPath()
    {
        FlagPath = string.Empty;
        return this;
    }
}