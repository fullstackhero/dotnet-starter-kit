namespace FSH.Framework.Core.Context;

public interface ICorrelationIdInitializer
{
    void SetCorrelationId(string correlationId);
}
