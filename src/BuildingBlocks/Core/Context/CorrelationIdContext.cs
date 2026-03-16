namespace FSH.Framework.Core.Context;

public class CorrelationIdContext : ICorrelationIdContext, ICorrelationIdInitializer
{
    private string? _correlationId;

    public string? CorrelationId => _correlationId;

    public void SetCorrelationId(string correlationId)
    {
        _correlationId = correlationId;
    }
}
