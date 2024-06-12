using MediatR;

namespace FSH.WebApi.Catalog.Application.Products.Update.v1;
public class UpdateProductRequest : IRequest<UpdateProductResponse>
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public UpdateProductRequest(Guid id, string Name, string Description, decimal Price)
    {
        this.Id = id;
        this.Name = Name;
        this.Description = Description;
        this.Price = Price;
    }
}
