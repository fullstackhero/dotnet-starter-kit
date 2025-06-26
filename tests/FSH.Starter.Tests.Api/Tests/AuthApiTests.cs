using FluentAssertions;
using Xunit.Abstractions;
using System.Net;
using FSH.Starter.Tests.Api.Infrastructure;
using FSH.Starter.Tests.Shared;

namespace FSH.Starter.Tests.Api.Tests;

public class AuthApiTests : ApiTestBase
{
    public AuthApiTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task HealthCheck_Should_Return_Success()
    {
        LogTestStart(Output, "API Health Check");

        // Act
        var response = await GetAsync("api/v1/auth/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Auth API is working");

        LogTestEnd(Output, "API Health Check", true);
    }

    [Fact]
    public async Task MernisTest_With_ValidData_Should_Return_Success()
    {
        LogTestStart(Output, "MERNIS Test with Valid Data");

        // Arrange
        var realPersons = TestDataManager.Persons.GetRealPersonsFromConfig();
        if (!realPersons.Any())
        {
            Output.WriteLine("⚠️ No real person data found in config. Skipping test.");
            return;
        }

        var testPerson = realPersons.First(p => p.ExpectedMernisResult);
        var mernisRequest = new TestMernisRequest
        {
            Tckn = testPerson.Tckn,
            FirstName = testPerson.FirstName,
            LastName = testPerson.LastName,
            BirthYear = DateTime.Parse(testPerson.BirthDate).Year
        };

        // Act
        var response = await PostJsonAsync("api/v1/auth/test-mernis", mernisRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("true").Or.Contain("successful");

        LogTestEnd(Output, "MERNIS Test with Valid Data", true);
    }

    [Fact]
    public async Task MernisTest_With_InvalidData_Should_Return_Failure()
    {
        LogTestStart(Output, "MERNIS Test with Invalid Data");

        // Arrange
        var realPersons = TestDataManager.Persons.GetRealPersonsFromConfig();
        var invalidPerson = realPersons.FirstOrDefault(p => !p.ExpectedMernisResult);
        
        if (invalidPerson == null)
        {
            // Generate invalid test data
            invalidPerson = TestDataManager.Persons.GenerateInvalidTcknPerson();
        }

        var mernisRequest = new TestMernisRequest
        {
            Tckn = invalidPerson.Tckn,
            FirstName = invalidPerson.FirstName,
            LastName = invalidPerson.LastName,
            BirthYear = DateTime.Parse(invalidPerson.BirthDate).Year
        };

        // Act
        var response = await PostJsonAsync("api/v1/auth/test-mernis", mernisRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("false").Or.Contain("failed");

        LogTestEnd(Output, "MERNIS Test with Invalid Data", true);
    }

    [Theory]
    [InlineData("InvalidTckn", "1234567890")] // 10 digits
    [InlineData("InvalidTckn", "00000000000")] // All zeros
    [InlineData("InvalidTckn", "ABCDEFGHIJK")] // Non-numeric
    public async Task RegisterRequest_With_InvalidTckn_Should_Return_ValidationError(string testType, string invalidTckn)
    {
        LogTestStart(Output, $"Register with {testType}: {invalidTckn}");

        // Arrange
        var fakePerson = TestDataManager.Persons.GenerateFakePerson();
        fakePerson.Tckn = invalidTckn;
        var registerRequest = TestDataManager.Api.CreateRegisterRequest(fakePerson);

        // Act
        var response = await PostJsonAsync("api/v1/auth/register-request", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("TC Kimlik").Or.Contain("TCKN").Or.Contain("kimlik");

        LogTestEnd(Output, $"Register with {testType}", true);
    }

    [Theory]
    [InlineData("InvalidPhone", "1234567890")] // Doesn't start with 5
    [InlineData("InvalidPhone", "5123")] // Too short
    [InlineData("InvalidPhone", "51234567890123")] // Too long
    public async Task RegisterRequest_With_InvalidPhone_Should_Return_ValidationError(string testType, string invalidPhone)
    {
        LogTestStart(Output, $"Register with {testType}: {invalidPhone}");

        // Arrange
        var fakePerson = TestDataManager.Persons.GenerateFakePerson();
        fakePerson.PhoneNumber = invalidPhone;
        var registerRequest = TestDataManager.Api.CreateRegisterRequest(fakePerson);

        // Act
        var response = await PostJsonAsync("api/v1/auth/register-request", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("telefon").Or.Contain("phone");

        LogTestEnd(Output, $"Register with {testType}", true);
    }

    [Theory]
    [InlineData("WeakPassword", "123")] // Too short
    [InlineData("WeakPassword", "password")] // No uppercase, numbers, special chars
    [InlineData("WeakPassword", "PASSWORD123")] // No lowercase, special chars
    public async Task RegisterRequest_With_WeakPassword_Should_Return_ValidationError(string testType, string weakPassword)
    {
        LogTestStart(Output, $"Register with {testType}: {weakPassword}");

        // Arrange
        var fakePerson = TestDataManager.Persons.GenerateFakePerson();
        fakePerson.Password = weakPassword;
        var registerRequest = TestDataManager.Api.CreateRegisterRequest(fakePerson);

        // Act
        var response = await PostJsonAsync("api/v1/auth/register-request", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("şifre").Or.Contain("password");

        LogTestEnd(Output, $"Register with {testType}", true);
    }

    [Fact]
    public async Task RegisterRequest_With_Under18_Should_Return_ValidationError()
    {
        LogTestStart(Output, "Register with Under 18 Age");

        // Arrange
        var underAgePerson = TestDataManager.Persons.GenerateUnder18Person();
        var registerRequest = TestDataManager.Api.CreateRegisterRequest(underAgePerson);

        // Act
        var response = await PostJsonAsync("api/v1/auth/register-request", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("18").Or.Contain("yaş");

        LogTestEnd(Output, "Register with Under 18 Age", true);
    }

    [Fact]
    public async Task RegisterRequest_With_ValidRealData_Should_Succeed()
    {
        LogTestStart(Output, "Register with Valid Real Data");

        // Arrange
        var realPersons = TestDataManager.Persons.GetRealPersonsFromConfig();
        if (!realPersons.Any(p => p.ExpectedMernisResult))
        {
            Output.WriteLine("⚠️ No valid real person data found in config. Skipping test.");
            return;
        }

        var validPerson = realPersons.First(p => p.ExpectedMernisResult);
        
        // Make email and phone unique for this test
        validPerson.Email = $"test_{Guid.NewGuid():N}@example.com";
        validPerson.PhoneNumber = TestDataManager.Validation.GenerateValidPhoneNumber();
        
        var registerRequest = TestDataManager.Api.CreateRegisterRequest(validPerson);

        // Act
        var response = await PostJsonAsync("api/v1/auth/register-request", registerRequest);

        // Assert
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            if (errorContent.Contains("already exists") || errorContent.Contains("zaten mevcut"))
            {
                Output.WriteLine("ℹ️ User already exists - this is expected for real data");
                LogTestEnd(Output, "Register with Valid Real Data", true);
                return;
            }
        }

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("success").Or.Contain("başarı");

        LogTestEnd(Output, "Register with Valid Real Data", true);
    }

    [Fact]
    public async Task FullRegistrationFlow_With_FakeData_Should_Complete()
    {
        LogTestStart(Output, "Full Registration Flow with Fake Data");

        // Arrange
        var fakePerson = TestDataManager.Persons.GenerateFakePerson();
        var registerRequest = TestDataManager.Api.CreateRegisterRequest(fakePerson);

        // Act 1: Register request
        var registerResponse = await PostJsonAsync("api/v1/auth/register-request", registerRequest);

        // Assert 1: Registration should start successfully
        registerResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        
        var registerContent = await registerResponse.Content.ReadAsStringAsync();
        registerContent.Should().Contain("success").Or.Contain("başarı").Or.Contain("SMS");

        // Act 2: Verify registration with SMS code
        var verifyRequest = new VerifyRegistrationRequest
        {
            PhoneNumber = fakePerson.PhoneNumber,
            OtpCode = TestDataManager.Configuration.GetMockOtpCode()
        };

        var verifyResponse = await PostJsonAsync("api/v1/auth/verify-registration", verifyRequest);

        // Assert 2: Verification should complete registration
        verifyResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        
        var verifyContent = await verifyResponse.Content.ReadAsStringAsync();
        verifyContent.Should().Contain("success").Or.Contain("başarı").Or.Contain("tamamland");

        // Act 3: Login with registered user
        var loginRequest = new LoginRequest
        {
            TcknOrMemberNumber = fakePerson.Tckn,
            Password = fakePerson.Password
        };

        var loginResponse = await PostJsonAsync("api/v1/auth/login", loginRequest);

        // Assert 3: Login should work
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        loginContent.Should().Contain("token").Or.Contain("access");

        LogTestEnd(Output, "Full Registration Flow with Fake Data", true);
    }

    [Fact]
    public async Task InvalidTestCases_From_Config_Should_Fail_Appropriately()
    {
        LogTestStart(Output, "Invalid Test Cases from Configuration");

        // Arrange
        var invalidCases = TestDataManager.Persons.GetInvalidPersonsFromConfig();
        
        if (!invalidCases.Any())
        {
            Output.WriteLine("⚠️ No invalid test cases found in config. Skipping test.");
            return;
        }

        var successCount = 0;
        var totalCount = invalidCases.Count;

        // Act & Assert for each invalid case
        foreach (var invalidCase in invalidCases)
        {
            Output.WriteLine($"🧪 Testing: Invalid TCKN {invalidCase.Tckn}");
            
            var registerRequest = TestDataManager.Api.CreateRegisterRequest(invalidCase);

            var response = await PostJsonAsync("api/v1/auth/register-request", registerRequest);

            // Should return validation error
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var content = await response.Content.ReadAsStringAsync();
                if (content.Contains("TC") || content.Contains("kimlik") || content.Contains("TCKN"))
                {
                    Output.WriteLine($"✅ Invalid TCKN {invalidCase.Tckn} - Failed as expected");
                    successCount++;
                }
                else
                {
                    Output.WriteLine($"❌ Invalid TCKN {invalidCase.Tckn} - Wrong error message");
                    Output.WriteLine($"   Actual: {content}");
                }
            }
            else
            {
                Output.WriteLine($"❌ Invalid TCKN {invalidCase.Tckn} - Should have failed but didn't");
            }
        }

        // Overall assertion
        successCount.Should().Be(totalCount, 
            $"All {totalCount} invalid test cases should fail with appropriate error messages");

        LogTestEnd(Output, "Invalid Test Cases from Configuration", successCount == totalCount);
    }
} 