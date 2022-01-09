namespace DN.WebApi.Infrastructure.Multitenancy;

public interface IMakeSecureConnectionString
{
    string? MakeSecure(string? connectionString, string? dbProvider);
}