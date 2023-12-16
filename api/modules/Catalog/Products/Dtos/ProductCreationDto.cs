namespace FSH.WebApi.Modules.Catalog.Products.Dtos;

public sealed record ProductCreationDto(string? name, decimal price, string? description = null);
