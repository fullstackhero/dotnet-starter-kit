namespace FSH.Starter.Tests.Unit.Controllers
{
    using System;
    using System.IO;
    using System.Text.Json;
    using Xunit;

    public class PersonalDataTests
    {
        [Fact]
        public void Should_Read_Personal_Data_From_Json()
        {
            var testDataPath = "testdata/personal_data.json";
            
            if (!File.Exists(testDataPath))
            {
                throw new FileNotFoundException(
                    $"Test data file not found: {testDataPath}\n" +
                    "This file contains sensitive test data and is not committed to git.\n" +
                    "For local development, create the file manually.\n" +
                    "For CI/CD, this should be provided via GitHub secrets."
                );
            }

            var jsonContent = File.ReadAllText(testDataPath);

            var data = JsonSerializer.Deserialize<PersonalData[]>(jsonContent);
            
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
