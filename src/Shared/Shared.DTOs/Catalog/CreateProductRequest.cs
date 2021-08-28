namespace DN.WebApi.Shared.DTOs.Catalog
{
    public class CreateProductRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Rate { get; set; }
    }
}