﻿namespace FSH.WebApi.Domain.Geo;

public class State : AuditableEntity, IAggregateRoot
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

    public string? Latitude { get; private set; }
    public string? Longitude { get; private set; }

    public string? Metropolis { get; private set; }

    public DefaultIdType? TypeId { get; private set; }
    public virtual GeoAdminUnit? Type { get; private set; }

    public DefaultIdType CountryId { get; private set; } = default!;
    public virtual Country Country { get; private set; } = default!;
    public virtual ICollection<Province> Provinces { get; private set; } = default!;

    public State(
        int order,
        string code,
        string name,
        string? description,
        bool isActive,
        string? fullName,
        string? nativeName,
        string? fullNativeName,
        int? numericCode,
        string? latitude,
        string? longitude,
        string? metropolis,
        DefaultIdType? typeId,
        DefaultIdType countryId)
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

        Latitude = latitude;
        Longitude = longitude;
        Metropolis = metropolis;

        TypeId = (typeId == DefaultIdType.Empty) ? null : typeId;
        CountryId = countryId;
    }

    public State()
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
                null,
                null,
                DefaultIdType.Empty )
        {
        }

    public State Update(
        int? order,
        string? code,
        string? name,
        string? description,
        bool? isActive,
        string? fullName,
        string? nativeName,
        string? fullNativeName,
        int? numericCode,
        string? latitude,
        string? longitude,
        string? metropolis,
        DefaultIdType? typeId,
        DefaultIdType? countryId)
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

        if (latitude is not null && Latitude?.Equals(latitude) is not true) Latitude = latitude;
        if (longitude is not null && Longitude?.Equals(longitude) is not true) Longitude = longitude;
        if (metropolis is not null && Metropolis?.Equals(metropolis) is not true) Metropolis = metropolis;

        if (typeId.HasValue && typeId.Value != DefaultIdType.Empty && !TypeId.Equals(typeId.Value)) TypeId = typeId.Value;
        if (countryId.HasValue && countryId.Value != DefaultIdType.Empty && !CountryId.Equals(countryId.Value)) CountryId = countryId.Value;

        return this;
    }
}