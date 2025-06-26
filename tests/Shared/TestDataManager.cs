using Bogus;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace FSH.Starter.Tests.Shared;

public static class TestDataManager
{
    private static readonly Lazy<IConfiguration> _configuration = new(() => LoadConfiguration());
    private static IConfiguration Configuration => _configuration.Value;
    
    private static IConfiguration LoadConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(GetProjectRoot())
            .AddJsonFile("testsettings.shared.json", optional: false)
            .AddJsonFile("testsettings.json", optional: true) // Override with local settings if exists
            .AddEnvironmentVariables("FSH_TEST_");
            
        return builder.Build();
    }
    
    private static string GetProjectRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir != null && !File.Exists(Path.Combine(currentDir, "testsettings.shared.json")))
        {
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }
        return currentDir ?? throw new InvalidOperationException("Could not find project root with testsettings.shared.json");
    }

    public static class Persons
    {
        private static readonly Faker<FakePerson> _personFaker = new Faker<FakePerson>("tr")
            .RuleFor(p => p.FirstName, f => f.Name.FirstName())
            .RuleFor(p => p.LastName, f => f.Name.LastName())
            .RuleFor(p => p.Email, f => f.Internet.Email())
            .RuleFor(p => p.PhoneNumber, f => GenerateValidPhoneNumber())
            .RuleFor(p => p.Tckn, f => GenerateValidTCKN())
            .RuleFor(p => p.Password, f => "TestPass123!")
            .RuleFor(p => p.BirthDate, f => f.Date.Between(DateTime.Now.AddYears(-65), DateTime.Now.AddYears(-18)).ToString("yyyy-MM-dd"))
            .RuleFor(p => p.ProfessionId, f => f.Random.Int(1, 5))
            .RuleFor(p => p.ExpectedMernisResult, f => true);

        public static FakePerson GenerateFakePerson() => _personFaker.Generate();
        
        public static FakePerson GenerateUnder18Person()
        {
            var person = GenerateFakePerson();
            person.BirthDate = DateTime.Now.AddYears(-16).ToString("yyyy-MM-dd");
            return person;
        }
        
        public static FakePerson GenerateInvalidTcknPerson()
        {
            var person = GenerateFakePerson();
            person.Tckn = "11111111110"; // Invalid TCKN
            person.ExpectedMernisResult = false;
            return person;
        }

        public static List<FakePerson> GetRealPersonsFromConfig()
        {
            var realPersonsSection = Configuration.GetSection("TestData:RealPersons");
            var realPersons = new List<FakePerson>();
            
            foreach (var section in realPersonsSection.GetChildren())
            {
                var person = new FakePerson
                {
                    Tckn = section["Tckn"] ?? "",
                    FirstName = section["FirstName"] ?? "",
                    LastName = section["LastName"] ?? "",
                    BirthDate = section["BirthDate"] ?? "",
                    PhoneNumber = section["PhoneNumber"] ?? "",
                    Email = section["Email"] ?? "",
                    Password = section["Password"] ?? "",
                    ProfessionId = int.Parse(section["ProfessionId"] ?? "1"),
                    ExpectedMernisResult = bool.Parse(section["ExpectedMernisResult"] ?? "true")
                };
                realPersons.Add(person);
            }
            
            return realPersons;
        }
        
        public static List<FakePerson> GetInvalidPersonsFromConfig()
        {
            var invalidPersonsSection = Configuration.GetSection("TestData:InvalidPersons");
            var invalidPersons = new List<FakePerson>();
            
            foreach (var section in invalidPersonsSection.GetChildren())
            {
                var person = new FakePerson
                {
                    Tckn = section["Tckn"] ?? "",
                    FirstName = section["FirstName"] ?? "",
                    LastName = section["LastName"] ?? "",
                    BirthDate = section["BirthDate"] ?? "",
                    PhoneNumber = section["PhoneNumber"] ?? "",
                    Email = section["Email"] ?? "",
                    Password = section["Password"] ?? "",
                    ProfessionId = int.Parse(section["ProfessionId"] ?? "1"),
                    ExpectedMernisResult = bool.Parse(section["ExpectedMernisResult"] ?? "false")
                };
                invalidPersons.Add(person);
            }
            
            return invalidPersons;
        }
    }

    public static class Users
    {
        public static List<TestUser> GetTestUsersFromConfig()
        {
            var testUsersSection = Configuration.GetSection("TestData:TestUsers");
            var testUsers = new List<TestUser>();
            
            foreach (var section in testUsersSection.GetChildren())
            {
                var user = new TestUser
                {
                    Email = section["Email"] ?? "",
                    Username = section["Username"] ?? "",
                    PhoneNumber = section["PhoneNumber"] ?? "",
                    Tckn = section["Tckn"] ?? "",
                    Password = section["Password"] ?? "",
                    FirstName = section["FirstName"] ?? "",
                    LastName = section["LastName"] ?? "",
                    ProfessionId = int.Parse(section["ProfessionId"] ?? "1"),
                    BirthDate = section["BirthDate"] ?? "",
                    Roles = section.GetSection("Roles").Get<string[]>() ?? Array.Empty<string>()
                };
                testUsers.Add(user);
            }
            
            return testUsers;
        }
    }

    public static class Api
    {
        public static object CreateRegisterRequest(FakePerson person)
        {
            return new
            {
                email = person.Email,
                phoneNumber = person.PhoneNumber,
                tckn = person.Tckn,
                password = person.Password,
                firstName = person.FirstName,
                lastName = person.LastName,
                birthDate = DateTime.Parse(person.BirthDate).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                professionId = person.ProfessionId,
                marketingConsent = true,
                electronicCommunicationConsent = true,
                membershipAgreementConsent = true
            };
        }
        
        public static object CreateLoginRequest(string identifier, string password)
        {
            return new
            {
                identifier = identifier,
                password = password
            };
        }
        
        public static object CreateVerifyRequest(string phoneNumber, string otpCode)
        {
            return new
            {
                phoneNumber = phoneNumber,
                otpCode = otpCode
            };
        }
    }

    public static class Validation
    {
        public static string GenerateValidTCKN()
        {
            var random = new Random();
            var first9Digits = new int[9];
            
            for (int i = 0; i < 9; i++)
            {
                first9Digits[i] = random.Next(1, 10);
            }
            
            var sumOdd = first9Digits[0] + first9Digits[2] + first9Digits[4] + first9Digits[6] + first9Digits[8];
            var sumEven = first9Digits[1] + first9Digits[3] + first9Digits[5] + first9Digits[7];
            
            var digit10 = ((sumOdd * 7) - sumEven) % 10;
            var digit11 = (sumOdd + sumEven + digit10) % 10;
            
            return string.Join("", first9Digits) + digit10 + digit11;
        }
        
        public static string GenerateValidPhoneNumber()
        {
            var random = new Random();
            return "5" + random.Next(100000000, 999999999);
        }
        
        public static string GenerateInvalidTCKN(string type = "invalid_format")
        {
            return type switch
            {
                "too_short" => "123456789",
                "all_zeros" => "00000000000",
                "all_ones" => "11111111111",
                "contains_letters" => "1234567890A",
                _ => "1234567890" // 10 digits instead of 11
            };
        }
        
        public static string GenerateInvalidPhoneNumber(string type = "invalid_format")
        {
            var random = new Random();
            return type switch
            {
                "not_starting_with_5" => "4" + random.Next(100000000, 999999999),
                "too_short" => "512345",
                "too_long" => "512345678901234",
                "contains_letters" => "5123456789A",
                _ => "1234567890"
            };
        }
    }

    public static class Configuration
    {
        public static string GetConnectionString() => 
            TestDataManager.Configuration.GetConnectionString("DefaultConnection") ?? 
            TestDataManager.Configuration["DatabaseSettings:ConnectionString"] ?? 
            "Server=localhost;Port=5434;Database=fsh_test;User Id=testuser;Password=testpass;";
            
        public static bool UseDevelopmentMode() => 
            bool.Parse(TestDataManager.Configuration["MernisService:UseDevelopmentMode"] ?? "true");
            
        public static string GetMockOtpCode() => 
            TestDataManager.Configuration["SmsService:MockOtpCode"] ?? "123456";
            
        public static bool UseRealData() => 
            bool.Parse(TestDataManager.Configuration["TestConfiguration:UseRealData"] ?? "false");
    }
}

public class FakePerson
{
    public string Tckn { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string BirthDate { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public int ProfessionId { get; set; }
    public bool ExpectedMernisResult { get; set; }
}

public class TestUser
{
    public string Email { get; set; } = "";
    public string Username { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Tckn { get; set; } = "";
    public string Password { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public int ProfessionId { get; set; }
    public string BirthDate { get; set; } = "";
    public string[] Roles { get; set; } = Array.Empty<string>();
}

public class TestMernisRequest
{
    public string Tckn { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public int BirthYear { get; set; }
}

public class VerifyRegistrationRequest
{
    public string PhoneNumber { get; set; } = "";
    public string OtpCode { get; set; } = "";
}

public class LoginRequest
{
    public string TcknOrMemberNumber { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RegisterRequest
{
    public string Email { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Tckn { get; set; } = "";
    public string Password { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public int ProfessionId { get; set; }
    public DateTime BirthDate { get; set; }
    public bool MarketingConsent { get; set; }
    public bool ElectronicCommunicationConsent { get; set; }
    public bool MembershipAgreementConsent { get; set; }
} 