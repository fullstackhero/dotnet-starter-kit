namespace FL_CRMS_ERP_WEBAPI.Infrastructure.Persistence.Configuration;

internal static class SchemaNames
{
    // TODO: figure out how to capitalize these only for Oracle
    public static string Auditing = nameof(Auditing); // "AUDITING";
    public static string Catalog = nameof(Catalog); // "CATALOG";
    public static string Identity = nameof(Identity); // "IDENTITY";
    public static string LeadData = nameof(LeadData);
    public static string Common = nameof(Common);
    public static string MultiTenancy = nameof(MultiTenancy); // "MULTITENANCY";
    public static string Enum = nameof(Enum); // "MULTITENANCY";
}