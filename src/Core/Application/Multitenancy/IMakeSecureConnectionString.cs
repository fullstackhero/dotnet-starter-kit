namespace FSH.WebApi.Application.Multitenancy;

public interface IMakeSecureConnectionString
{
    string? MakeSecure(string? connectionString, string? dbProvider = null);
}