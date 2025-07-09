namespace FSH.Starter.Tests.Unit.Services
{
    using System.Text.Json;
    using Xunit;

    public class PersonalDataServiceTests
    {
        [Fact]
        public void Should_Read_Personal_Data_From_Json()
        {
            var json = System.IO.File.ReadAllText("testdata/personal_data.json");
            var data = JsonSerializer.Deserialize<PersonalData[]>(json);
            Assert.NotNull(data);
            Assert.NotEmpty(data);
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
