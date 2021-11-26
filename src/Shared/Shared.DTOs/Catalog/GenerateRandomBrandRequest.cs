namespace DN.WebApi.Shared.DTOs.Catalog;

public class GenerateRandomBrandRequest : IMustBeValid
{
    public int NSeed { get; set; }
}