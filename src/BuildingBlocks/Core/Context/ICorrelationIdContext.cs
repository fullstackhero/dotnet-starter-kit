namespace FSH.Framework.Core.Context;

public interface ICorrelationIdContext
{
    string? CorrelationId { get; }
}
