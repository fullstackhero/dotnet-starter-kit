namespace FSH.Modules.Auditing.Contracts;

public static class ExceptionSeverityClassifier
{
    public static AuditSeverity Classify(Exception ex) =>
        ex switch
        {
            OperationCanceledException => AuditSeverity.Information,
            UnauthorizedAccessException => AuditSeverity.Warning,
            _ => AuditSeverity.Error
        };
}