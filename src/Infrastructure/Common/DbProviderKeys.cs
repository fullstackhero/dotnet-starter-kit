namespace FSH.WebApi.Infrastructure.Common;

internal class DbProviderKeys
{
    public const string Npgsql = "postgresql";
    public const string SqlServer = "mssql";
    public const string MySql = "mysql";
    public const string Oracle = "oracle";
    public const string SqLite = "sqlite";
    
    public static string GetMigratorAssemblyNameFromDbProviderKey(string dbProviderKey)
    {
        return dbProviderKey switch
        {
            Npgsql => "Migrators.PostgreSQL",
            SqlServer => "Migrators.MSSQL",
            MySql => "Migrators.MySQL",
            Oracle => "Migrators.Oracle",
            SqlLite => "Migrators.SqLite",
            _ => throw new NotSupportedException($"{dbProviderKey} is not supported")
        };
    }
}
