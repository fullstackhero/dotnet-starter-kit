namespace FSH.WebApi.Infrastructure.Persistence.Configuration;

internal static class SchemaNames
{
    // TODO: figure out how to capitalize these only for Oracle
    public static string Auditing = nameof(Auditing); // "AUDITING";
    public static string Identity = nameof(Identity); // "IDENTITY";
    public static string MultiTenancy = nameof(MultiTenancy); // "MULTITENANCY";
    public static string Catalog = nameof(Catalog); // "CATALOG";

    public static string Settings = nameof(Settings);

    public static string Geo = nameof(Geo);

    public static string Organization = nameof(Organization);
    public static string People = nameof(People);
    public static string Purchase = nameof(Purchase);

    public static string Property = nameof(Property);
    public static string Production = nameof(Production);

    public static string Elearning = nameof(Elearning);
}