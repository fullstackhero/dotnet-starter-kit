namespace FSH.Starter.Tests.Unit.Controllers
{
    using System.Text.Json;
    using Xunit;

    public class PersonalDataTests
    {
        [Fact]
        public void Should_Read_Personal_Data_From_Json()
        {
            // Arrange
            var json = System.IO.File.ReadAllText("testdata/personal_data.json");
            var data = JsonSerializer.Deserialize<PersonalData[]>(json);

            // Assert
            Assert.NotNull(data);
            Assert.NotEmpty(data);
            Assert.All(data, d =>
            {
                Assert.False(string.IsNullOrWhiteSpace(d.firstName));
                Assert.False(string.IsNullOrWhiteSpace(d.lastName));
                Assert.False(string.IsNullOrWhiteSpace(d.tckn));
                Assert.False(string.IsNullOrWhiteSpace(d.birthDate));
            });
        }

        private class PersonalData
        {
            public string? firstName { get; set; }
            public string? lastName { get; set; }
            public string? tckn { get; set; }
            public string? birthDate { get; set; }
        }
    }
}
