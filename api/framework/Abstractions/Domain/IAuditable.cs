namespace FSH.Framework.Abstractions.Domain;

public interface IAuditable
{
    DateTime Created { get; }
    int? CreatedBy { get; }
    DateTime? LastModified { get; }
    int? LastModifiedBy { get; }
}
