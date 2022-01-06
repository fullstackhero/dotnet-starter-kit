using DN.WebApi.Application.Multitenancy;
using Infrastructure.Test.Multitenancy.Fixtures;
using Xunit;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace DN.WebApi.Infrastructure.Multitenancy.Tests
{
    public class MakeSecureConnectionStringTests : TestBed<TestFixture> //TestBedFixture
    {
        private const string Mssql = "Data Source=(localdb)\\mssqllocaldb;Initial Catalog=fullStackHeroDb;MultipleActiveResultSets=True;";
        private readonly IMakeSecureConnectionString _makeSecureConnectionString;

        public MakeSecureConnectionStringTests(ITestOutputHelper testOutputHelper, TestFixture fixture)
            : base(testOutputHelper, fixture)
        {
            _makeSecureConnectionString = _fixture.GetService<IMakeSecureConnectionString>(_testOutputHelper);
        }

        [Fact]
        public void GetSecureConnectionStringTest()
        {
            string mssql1 = Mssql + ";Integrated Security=True;";
            string mssql2 = Mssql + ";user id=sa;password=pass;";

            string? res1 = _makeSecureConnectionString.MakeSecure("mssql", mssql1);
            string? check1 = _makeSecureConnectionString.MakeSecure("mssql", res1);

            Assert.True(check1?.Equals(res1, StringComparison.InvariantCultureIgnoreCase), "MSSQL: CASE 1 - Integrated Security"); // CASE 1

            string? res2 = _makeSecureConnectionString.MakeSecure("mssql", mssql2);
            string? check2 = _makeSecureConnectionString.MakeSecure("mssql", res2);

            Assert.True(check2?.Equals(res2, StringComparison.InvariantCultureIgnoreCase), "MSSQL: CASE 2 - Credentials"); // CASE 2

        }


        protected override void Clear() { }

        protected override ValueTask DisposeAsyncCore()
            => new();
    }
}