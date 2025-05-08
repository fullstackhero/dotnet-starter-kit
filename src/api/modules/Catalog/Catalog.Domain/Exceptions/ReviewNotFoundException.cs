namespace FSH.Starter.WebApi.Catalog.Domain.Exceptions;

public class ReviewNotFoundException : Exception
{
    public ReviewNotFoundException(Guid id)
        : base($"Review with ID {id} was not found.")
    {
    }
}
