namespace DN.WebApi.Application.Multitenancy;

public interface IMakeSecureConnectionString
{
    string? MakeSecure(string? connectionString, string? dbProvider);
}