using Xunit;

namespace DN.WebApi.Infrastructure.Multitenancy.Tests
{
    public class HideSecureConnectionStringTests
    {
        private const string Mssql = "Data Source=(localdb)\\mssqllocaldb;Initial Catalog=fullStackHeroDb;MultipleActiveResultSets=True;";
        [Fact]
        public void GetSecureConnectionStringTest()
        {
            string mssql1 = Mssql + ";Integrated Security=True;";
            string mssql2 = Mssql + ";user id=sa;password=pass;";

            string? res1 = HideSecureConnectionString.GetSecureConnectionString("mssql", mssql1);
            string? check1 = HideSecureConnectionString.GetSecureConnectionString("mssql", res1);

            Assert.True(check1?.Equals(res1, StringComparison.InvariantCultureIgnoreCase), "MSSQL: CASE 1 - Integrated Security"); // CASE 1

            string? res2 = HideSecureConnectionString.GetSecureConnectionString("mssql", mssql2);
            string? check2 = HideSecureConnectionString.GetSecureConnectionString("mssql", res2);

            Assert.True(check2?.Equals(res2, StringComparison.InvariantCultureIgnoreCase), "MSSQL: CASE 2 - Credentials"); // CASE 2

        }
    }
}