namespace FSH.Framework.Core.Domain.Interfaces;
public interface ISoftDelete
{
    bool IsDeleted { get; }
    DateTime? DeletedOnUtc { get; }
    string? DeletedBy { get; }
}