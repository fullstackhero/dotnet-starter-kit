namespace FSH.Framework.Core.Domain.Interfaces;
public interface IAuditable
{
    string? CreatedBy { get; }
    DateTime CreatedOnUtc { get; }
    string? LastModifiedBy { get; }
    DateTime? LastModifiedOnUtc { get; }
}