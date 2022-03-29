namespace FSH.WebApi.Domain.Catalog;

public class GameType : AuditableEntity, IAggregateRoot
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string Rules { get; set; }
    


    public GameType(string name, string? description,string rules )
    {
        Name = name;
        Description = description;
        Rules = rules;
        
    }

    public GameType Update(string? name, string? description,string rules)
    {
        if (name is not null && Name?.Equals(name) is not true) Name = name;
        if (description is not null && Description?.Equals(description) is not true) Description = description;
        if (rules is not null && Rules?.Equals(rules) is not true) Rules = rules;
        
        return this;
    }
}