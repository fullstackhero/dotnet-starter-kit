namespace FSH.Framework.Auditing.Contracts;

public static class AuditingConstants
{
    public const string SchemaName = "auditing";
    public const string ModuleName = "Auditing";

    public static class Permissions
    {
        public const string View = "Permissions.Auditing.View";
    }

    public static class Routes
    {
        public const string Base = "/v1/auditing";
        public const string GetUserLogs = $"{Base}/logs/{{userId:guid}}";
    }
}