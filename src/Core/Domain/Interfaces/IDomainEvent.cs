namespace FSH.Framework.Core.Domain.Interfaces;
public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}