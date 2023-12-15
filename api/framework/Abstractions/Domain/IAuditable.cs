namespace FSH.Framework.Abstractions.Domain;

public interface IAuditable
{
    DateTime Created { get; }
    Guid CreatedBy { get; }
    DateTime? LastModified { get; }
    Guid? LastModifiedBy { get; }
}
