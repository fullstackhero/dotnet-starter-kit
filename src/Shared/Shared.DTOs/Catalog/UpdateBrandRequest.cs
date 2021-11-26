namespace DN.WebApi.Shared.DTOs.Catalog;

public class UpdateBrandRequest : IMustBeValid
{
    public string Name { get; set; }
    public string Description { get; set; }
}