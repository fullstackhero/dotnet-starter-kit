namespace FSH.WebAPI.Application.Multitenancy;

public interface IMakeSecureConnectionString
{
    string? MakeSecure(string? connectionString, string? dbProvider = null);
}