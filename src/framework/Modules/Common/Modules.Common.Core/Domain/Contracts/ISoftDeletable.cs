namespace FSH.Modules.Common.Core.Domain.Contracts;

public interface ISoftDeletable
{
    DateTimeOffset? Deleted { get; set; }
    Guid? DeletedBy { get; set; }
}