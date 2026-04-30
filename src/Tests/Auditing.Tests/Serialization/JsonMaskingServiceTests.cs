using FSH.Modules.Auditing;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Auditing.Tests.Serialization;

/// <summary>
/// Tests for JsonMaskingService - security critical functionality
/// that masks sensitive fields before audit persistence.
/// </summary>
public sealed class JsonMaskingServiceTests
{
    private readonly JsonMaskingService _sut = new();

    #region Basic Field Masking

    [Fact]
    public void ApplyMasking_Should_Mask_Password_Field()
    {
        // Arrange
        var payload = new { username = "john", password = "secret123" };

        // Act
        var result = _sut.ApplyMasking(payload);

        // Assert
        var json = result.Payload as JsonNode;
        json.ShouldNotBeNull();
        json["password"]?.GetValue<string>().ShouldBe("****");
        json["username"]?.GetValue<string>().ShouldBe("john");
    }

    [Fact]
    public void ApplyMasking_Should_Mask_Secret_Field()
    {
        // Arrange
        var payload = new { apiSecret = "abc123", name = "test" };

        // Act
        var result = _sut.ApplyMasking(payload);

        // Assert
        var json = result.Payload as JsonNode;
        json.ShouldNotBeNull();
        json["apiSecret"]?.GetValue<string>().ShouldBe("****");
        json["name"]?.GetValue<string>().ShouldBe("test");
    }

    [Fact]
    public void ApplyMasking_Should_Mask_Token_Field()
    {
        // Arrange
        var payload = new { token = "jwt-token-value", userId = "user1" };

        // Act
        var result = _sut.ApplyMasking(payload);

        // Assert
        var json = result.Payload as JsonNode;
        json.ShouldNotBeNull();
        json["token"]?.GetValue<string>().ShouldBe("****");
    }

    [Fact]
    public void ApplyMasking_Should_Mask_Otp_Field()
    {
        // Arrange
        var payload = new { otp = "123456", email = "test@example.com" };

        // Act
        var result = _sut.ApplyMasking(payload);

        // Assert
        var json = result.Payload as JsonNode;
        json.ShouldNotBeNull();
        json["otp"]?.GetValue<string>().ShouldBe("****");
        json["email"]?.GetValue<string>().ShouldBe("test@example.com");
    }

    [Fact]
    public void ApplyMasking_Should_Mask_Pin_Field()
    {
        // Arrange
        var payload = new { pin = "1234", accountNumber = "ACC123" };

        // Act
        var result = _sut.ApplyMasking(payload);

        // Assert
        var json = result.Payload as JsonNode;
        json.ShouldNotBeNull();
        json["pin"]?.GetValue<string>().ShouldBe("****");
    }

    [Fact]
    public void ApplyMasking_Should_Mask_AccessToken_Field()
    {
        // Arrange
        var payload = new { accessToken = "access-token-value", scope = "read" };

        // Act
        var result = _sut.ApplyMasking(payload);

        // Assert
        var json = result.Payload as JsonNode;
        json.ShouldNotBeNull();
        json["accessToken"]?.GetValue<string>().ShouldBe("****");
        json["scope"]?.GetValue<string>().ShouldBe("read");
    }

    [Fact]
    public void ApplyMasking_Should_Mask_RefreshToken_Field()
    {
        // Arrange
        var payload = new { refreshToken = "refresh-token-value", expiresIn = 3600 };

        // Act
        var result = _sut.ApplyMasking(payload);

        // Assert
        var json = result.Payload as JsonNode;
        json.ShouldNotBeNull();
        json["refreshToken"]?.GetValue<string>().ShouldBe("****");
        json["expiresIn"]?.GetValue<int>().ShouldBe(3600);
    }

    #endregion

    #region Case Insensitivity

    [Theory]
    [InlineData("PASSWORD")]
    [InlineData("Password")]
    [InlineData("password")]
    [InlineData("passWord")]
    public void ApplyMasking_Should_Mask_Password_CaseInsensitive(string fieldName)
    {
        // Arrange
        var payload = new Dictionary<string, object> { [fieldName] = "secret123" };

        // Act
        var result = _sut.ApplyMasking(payload);

        // Assert
        var json = result.Payload as JsonNode;
        json.ShouldNotBeNull();
        json[fieldName]?.GetValue<string>().ShouldBe("****");
    }

    [Fact]
    public void ApplyMasking_Should_Mask_PartialMatch_UserPassword()
    {
        // Arrange
        var payload = new { userPassword = "secret123", userId = "user1" };

        // Act
        var result = _sut.ApplyMasking(payload);

        // Assert
        var json = result.Payload as JsonNode;
        json.ShouldNotBeNull();
        json["userPassword"]?.GetValue<string>().ShouldBe("****");
        json["userId"]?.GetValue<string>().ShouldBe("user1");
    }

    [Fact]
    public void ApplyMasking_Should_Mask_PartialMatch_ClientSecret()
    {
        // Arrange
        var payload = new { clientSecret = "secret-value", clientId = "client1" };

        // Act
        var result = _sut.ApplyMasking(payload);

        // Assert
        var json = result.Payload as JsonNode;
        json.ShouldNotBeNull();
        json["clientSecret"]?.GetValue<string>().ShouldBe("****");
        json["clientId"]?.GetValue<string>().ShouldBe("client1");
    }

    #endregion

    #region Nested Object Masking

    [Fact]
    public void ApplyMasking_Should_Mask_NestedObject_Password()
    {
        // Arrange
        var payload = new
        {
            user = new { name = "john", password = "secret123" }
        };

        // Act
        var result = _sut.ApplyMasking(payload);

        // Assert
        var json = result.Payload as JsonNode;
        json.ShouldNotBeNull();
        json["user"]?["password"]?.GetValue<string>().ShouldBe("****");
        json["user"]?["name"]?.GetValue<string>().ShouldBe("john");
    }

    [Fact]
    public void ApplyMasking_Should_Mask_DeeplyNested_Token()
    {
        // Arrange
        var payload = new
        {
            auth = new
            {
                credentials = new
                {
                    token = "deep-token-value",
                    type = "bearer"
                }
            }
        };

        // Act
        var result = _sut.ApplyMasking(payload);

        // Assert
        var json = result.Payload as JsonNode;
        json.ShouldNotBeNull();
        json["auth"]?["credentials"]?["token"]?.GetValue<string>().ShouldBe("****");
        json["auth"]?["credentials"]?["type"]?.GetValue<string>().ShouldBe("bearer");
    }

    [Fact]
    public void ApplyMasking_Should_Mask_Array_Elements_With_Password()
    {
        // Arrange
        var payload = new
        {
            users = new[]
            {
                new { name = "john", password = "pass1" },
                new { name = "jane", password = "pass2" }
            }
        };

        // Act
        var result = _sut.ApplyMasking(payload);

        // Assert
        var json = result.Payload as JsonNode;
        json.ShouldNotBeNull();
        var users = json["users"] as JsonArray;
        users.ShouldNotBeNull();
        users[0]?["password"]?.GetValue<string>().ShouldBe("****");
        users[1]?["password"]?.GetValue<string>().ShouldBe("****");
        users[0]?["name"]?.GetValue<string>().ShouldBe("john");
        users[1]?["name"]?.GetValue<string>().ShouldBe("jane");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ApplyMasking_Should_ReturnOriginal_When_Null()
    {
        // Arrange
        object? payload = null;

        // Act
        var result = _sut.ApplyMasking(payload!);

        // Assert
        // Null payload serializes to JSON null; nothing to mask, so the
        // service returns the original reference with a zero count.
        result.MaskedFieldCount.ShouldBe(0);
        result.Payload.ShouldBeNull();
    }

    [Fact]
    public void ApplyMasking_Should_NotMask_UnrelatedFields()
    {
        // Arrange
        var payload = new
        {
            username = "john",
            email = "john@example.com",
            age = 30,
            isActive = true
        };

        // Act
        var result = _sut.ApplyMasking(payload);

        // Assert
        var json = result.Payload as JsonNode;
        json.ShouldNotBeNull();
        json["username"]?.GetValue<string>().ShouldBe("john");
        json["email"]?.GetValue<string>().ShouldBe("john@example.com");
        json["age"]?.GetValue<int>().ShouldBe(30);
        json["isActive"]?.GetValue<bool>().ShouldBeTrue();
    }

    [Fact]
    public void ApplyMasking_Should_Handle_EmptyObject()
    {
        // Arrange
        var payload = new { };

        // Act
        var result = _sut.ApplyMasking(payload);

        // Assert
        var json = result.Payload as JsonNode;
        json.ShouldNotBeNull();
    }

    [Fact]
    public void ApplyMasking_Should_Handle_EmptyArray()
    {
        // Arrange
        var payload = new { items = Array.Empty<object>() };

        // Act
        var result = _sut.ApplyMasking(payload);

        // Assert
        var json = result.Payload as JsonNode;
        json.ShouldNotBeNull();
        var items = json["items"] as JsonArray;
        items.ShouldNotBeNull();
        items.Count.ShouldBe(0);
    }

    [Fact]
    public void ApplyMasking_Should_Handle_MixedTypes_InArray()
    {
        // Arrange
        var payload = new
        {
            data = new object[] { "string", 123, true, new { password = "secret" } }
        };

        // Act
        var result = _sut.ApplyMasking(payload);

        // Assert
        var json = result.Payload as JsonNode;
        json.ShouldNotBeNull();
        var data = json["data"] as JsonArray;
        data.ShouldNotBeNull();
        data[3]?["password"]?.GetValue<string>().ShouldBe("****");
    }

    [Fact]
    public void ApplyMasking_Should_Mask_AllSensitiveFields_InSingleObject()
    {
        // Arrange
        var payload = new
        {
            password = "pass",
            secret = "sec",
            token = "tok",
            otp = "123",
            pin = "456",
            accessToken = "at",
            refreshToken = "rt",
            normalField = "normal"
        };

        // Act
        var result = _sut.ApplyMasking(payload);

        // Assert
        var json = result.Payload as JsonNode;
        json.ShouldNotBeNull();
        json["password"]?.GetValue<string>().ShouldBe("****");
        json["secret"]?.GetValue<string>().ShouldBe("****");
        json["token"]?.GetValue<string>().ShouldBe("****");
        json["otp"]?.GetValue<string>().ShouldBe("****");
        json["pin"]?.GetValue<string>().ShouldBe("****");
        json["accessToken"]?.GetValue<string>().ShouldBe("****");
        json["refreshToken"]?.GetValue<string>().ShouldBe("****");
        json["normalField"]?.GetValue<string>().ShouldBe("normal");
    }

    #endregion
}