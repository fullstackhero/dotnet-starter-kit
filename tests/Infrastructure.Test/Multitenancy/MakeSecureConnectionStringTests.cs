using DN.WebApi.Application.Multitenancy;
using Infrastructure.Test.Multitenancy.Fixtures;
using Xunit;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace DN.WebApi.Infrastructure.Multitenancy.Tests
{
    public class MakeSecureConnectionStringTests : TestBed<TestFixture>
    {
        private const string Mssql = "Data Source=(localdb)\\mssqllocaldb;Initial Catalog=fullStackHeroDb;MultipleActiveResultSets=True;";

        private readonly IMakeSecureConnectionString _makeSecureConnectionString;

        public MakeSecureConnectionStringTests(ITestOutputHelper testOutputHelper, TestFixture fixture)
            : base(testOutputHelper, fixture)
        {
            _makeSecureConnectionString = _fixture.GetService<IMakeSecureConnectionString>(_testOutputHelper);
        }

        [Theory]
        [InlineData(Mssql + ";Integrated Security=True;", "MSSQL: CASE 1 - Integrated Security")]
        [InlineData(Mssql + ";user id=sa;password=pass;", "MSSQL: CASE 2 - Credentials")]
        public void MakeSecureTest(string mssql, string name)
        {
            string? res1 = _makeSecureConnectionString.MakeSecure("mssql", mssql);
            string? check1 = _makeSecureConnectionString.MakeSecure("mssql", res1);

            Assert.True(check1?.Equals(res1, StringComparison.InvariantCultureIgnoreCase), name);
        }

        protected override void Clear()
        {
        }

        protected override ValueTask DisposeAsyncCore()
            => new();
    }
}