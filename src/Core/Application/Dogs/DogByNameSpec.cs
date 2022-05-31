namespace FSH.WebApi.Application.Dogs;

public class DogByNameSpec : Specification<Dog>, ISingleResultSpecification
{
    public DogByNameSpec(string name) =>
        Query.Where(b => b.Name == name);
}