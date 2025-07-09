namespace FSH.Starter.Tests.Unit.Services
{
    using System;
    using System.IO;
    using System.Text.Json;
    using Xunit;

    public class PersonalDataServiceTests
    {
        [Fact]
        public void Should_Read_Personal_Data_From_Json()
        {
            // Try different path strategies to make this work in both local and CI environments
            string[] possiblePaths = {
                "testdata/personal_data.json",                                    // Current directory (works locally)
                Path.Combine(Directory.GetCurrentDirectory(), "testdata/personal_data.json"), // Absolute current dir
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "testdata/personal_data.json"), // Test output dir
                "../../testdata/personal_data.json",                            // Relative to test project
                "../../../testdata/personal_data.json"                          // Relative to solution
            };

            string? jsonContent = null;
            string? foundPath = null;

            foreach (var path in possiblePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    jsonContent = System.IO.File.ReadAllText(path);
                    foundPath = path;
                    break;
                }
            }

            if (jsonContent == null)
            {
                var currentDir = Directory.GetCurrentDirectory();
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var searchPaths = string.Join("\n  ", possiblePaths);
                
                throw new FileNotFoundException(
                    $"Could not find personal_data.json in any of the expected locations:\n  {searchPaths}\n\n" +
                    $"Current Directory: {currentDir}\n" +
                    $"Base Directory: {baseDir}"
                );
            }

            var data = JsonSerializer.Deserialize<PersonalData[]>(jsonContent);
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
