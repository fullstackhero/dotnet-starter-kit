using FSH.WebApi.Application.Common.Persistence;
using Xunit;

namespace FSH.WebApi.Infrastructure.Persistence.Tests;

public class ConnectionStringSecurerTests
{
    private const string Mssql = "Data Source=(localdb)\\mssqllocaldb;Initial Catalog=fullStackHeroDb;MultipleActiveResultSets=True;";
    private const string Mysql = "server=127.0.0.1;database=test";

    private readonly IConnectionStringSecurer _connectionStringSecurer;

    public ConnectionStringSecurerTests(IConnectionStringSecurer connectionStringSecurer) => _connectionStringSecurer = connectionStringSecurer;

    [Theory]
    [InlineData(Mssql + ";Integrated Security=True;", "mssql", false, "MSSQL: CASE 1 - Integrated Security")]
    [InlineData(Mssql + ";user id=root;password=12345;", "mssql", true, "MSSQL: CASE 2 - Credentials")]
    [InlineData(Mysql + ";uid=root;pwd=12345;", "mysql", true, "MYSQL: CASE 3 - Credentials")]
    public void MakeSecureTest(string connectionString, string dbProvider, bool containsCredentials, string name)
    {
        string? res1 = _connectionStringSecurer.MakeSecure(connectionString, dbProvider);
        string? check1 = _connectionStringSecurer.MakeSecure(res1, dbProvider);

        Assert.True(check1?.Equals(res1, StringComparison.InvariantCultureIgnoreCase), name); // don't know what this is actually testing?

        Assert.DoesNotContain("12345", check1);
        Assert.DoesNotContain("root", check1);

        if (containsCredentials)
        {
            Assert.Contains("*******", check1);
        }
    }
}