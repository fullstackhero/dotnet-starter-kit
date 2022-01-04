using Xunit;
using DN.WebApi.Infrastructure.Multitenancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DN.WebApi.Infrastructure.Multitenancy.Tests
{
    public class HideSecureConnectionStringTests
    {
        [Fact]
        public void GetSecureConnectionStringTest()
        {
            string mssql1 = "Data Source=(localdb)\\mssqllocaldb;Initial Catalog=fullStackHeroDb;Integrated Security=True;" +
                         "MultipleActiveResultSets=True";

            string? res1 = HideSecureConnectionString.GetSecureConnectionString("mssql", mssql1);
            Assert.Equal(res1, mssql1); // case 1

            string mssql2 = "Data Source=(localdb)\\mssqllocaldb;Initial Catalog=fullStackHeroDb;user id=sa;password=pass;" +
                         "MultipleActiveResultSets=True";
            string mssql3 = "Data Source=(localdb)\\mssqllocaldb;Initial Catalog=fullStackHeroDb;user id=*******;password=*******;" +
                         "MultipleActiveResultSets=True";

            string? res2 = HideSecureConnectionString.GetSecureConnectionString("mssql", mssql2);
            Assert.True(mssql3.Equals(res2, StringComparison.InvariantCultureIgnoreCase)); // case 2

            // Assert.True(false, "This test needs an implementation");
        }
    }
}