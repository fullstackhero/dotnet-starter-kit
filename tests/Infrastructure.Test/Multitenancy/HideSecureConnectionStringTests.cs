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
        [Fact()]
        public void GetSecureConnectionStringTest()
        {
            var mssql1 =
                "Data Source=(localdb)\\mssqllocaldb;Initial Catalog=fullStackHeroDb;Integrated Security=True;MultipleActiveResultSets=True";
            Assert.Equal(HideSecureConnectionString.GetSecureConnectionString("mssql", mssql1), mssql1);

            var mssql2 =
                "Data Source=(localdb)\\mssqllocaldb;Initial Catalog=fullStackHeroDb;user id=sa;password=pass;MultipleActiveResultSets=True";
            var mssql3 =
                "Data Source=(localdb)\\mssqllocaldb;Initial Catalog=fullStackHeroDb;user id=*******;password=*******;MultipleActiveResultSets=True";
            var msql4 = HideSecureConnectionString.GetSecureConnectionString("mssql", mssql2);
            Assert.True(mssql3.Equals(msql4,StringComparison.InvariantCultureIgnoreCase));

            //Assert.True(false, "This test needs an implementation");
        }
    }
}