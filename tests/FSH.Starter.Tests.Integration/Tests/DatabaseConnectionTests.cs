using FluentAssertions;
using FSH.Starter.Tests.Integration.Infrastructure;
using FSH.Starter.Tests.Shared;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace FSH.Starter.Tests.Integration.Tests;

public class DatabaseConnectionTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DatabaseConnectionTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Reset database before each test class
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Database_Should_Be_Accessible()
    {
        // Arrange & Act
        using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        // Assert
        connection.State.Should().Be(System.Data.ConnectionState.Open);
    }

    [Fact]
    public async Task Database_Should_Have_Required_Tables()
    {
        // Arrange
        using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        // Act
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = 'public' 
            AND table_type = 'BASE TABLE'
            ORDER BY table_name;";

        var tables = new List<string>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        // Assert
        tables.Should().Contain("users");
        tables.Should().Contain("roles");
        tables.Should().Contain("user_roles");
        tables.Should().Contain("professions");
    }

    [Fact]
    public async Task Database_Should_Have_Default_Roles()
    {
        // Arrange
        using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        // Act
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM roles ORDER BY name;";

        var roles = new List<string>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            roles.Add(reader.GetString(0));
        }

        // Assert
        roles.Should().Contain("Admin");
        roles.Should().Contain("User");
    }

    [Fact]
    public async Task Database_Should_Have_Test_Professions()
    {
        // Arrange
        using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        // Act
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM professions WHERE is_active = true;";

        var count = (long)(await command.ExecuteScalarAsync())!;

        // Assert
        count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task User_CRUD_Operations_Should_Work()
    {
        // Arrange
        using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        var testEmail = $"test_{Guid.NewGuid():N}@example.com";
        var testTckn = "12345678901";
        var testPhone = "5551234567";

        // Act 1: Insert user
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                INSERT INTO users (email, username, phone_number, tckn, password_hash, 
                                 first_name, last_name, profession_id, birth_date)
                VALUES (@email, @username, @phone, @tckn, @password, 
                        @firstName, @lastName, @professionId, @birthDate)
                RETURNING id;";

            command.Parameters.AddWithValue("email", testEmail);
            command.Parameters.AddWithValue("username", $"testuser_{Guid.NewGuid():N}"[..20]);
            command.Parameters.AddWithValue("phone", testPhone);
            command.Parameters.AddWithValue("tckn", testTckn);
            command.Parameters.AddWithValue("password", "hashed_password_here");
            command.Parameters.AddWithValue("firstName", "Test");
            command.Parameters.AddWithValue("lastName", "User");
            command.Parameters.AddWithValue("professionId", 1);
            command.Parameters.AddWithValue("birthDate", new DateTime(1990, 1, 1));

            var userId = await command.ExecuteScalarAsync();
            userId.Should().NotBeNull();
        }

        // Act 2: Read user
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT email, tckn, first_name FROM users WHERE email = @email;";
            command.Parameters.AddWithValue("email", testEmail);

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();

            // Assert
            reader.GetString(0).Should().Be(testEmail);
            reader.GetString(1).Should().Be(testTckn);
            reader.GetString(2).Should().Be("Test");
        }

        // Act 3: Update user
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                UPDATE users 
                SET first_name = @newFirstName 
                WHERE email = @email;";

            command.Parameters.AddWithValue("newFirstName", "Updated");
            command.Parameters.AddWithValue("email", testEmail);

            var affectedRows = await command.ExecuteNonQueryAsync();
            affectedRows.Should().Be(1);
        }

        // Act 4: Verify update
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT first_name FROM users WHERE email = @email;";
            command.Parameters.AddWithValue("email", testEmail);

            var firstName = await command.ExecuteScalarAsync() as string;
            firstName.Should().Be("Updated");
        }

        // Act 5: Delete user
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "DELETE FROM users WHERE email = @email;";
            command.Parameters.AddWithValue("email", testEmail);

            var affectedRows = await command.ExecuteNonQueryAsync();
            affectedRows.Should().Be(1);
        }
    }

    [Fact]
    public async Task Foreign_Key_Constraints_Should_Work()
    {
        // Arrange
        using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        // Act & Assert: Try to insert user with invalid profession_id
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO users (email, username, phone_number, tckn, password_hash, 
                             first_name, last_name, profession_id, birth_date)
            VALUES (@email, @username, @phone, @tckn, @password, 
                    @firstName, @lastName, @professionId, @birthDate);";

        command.Parameters.AddWithValue("email", $"test_{Guid.NewGuid():N}@example.com");
        command.Parameters.AddWithValue("username", $"testuser_{Guid.NewGuid():N}"[..20]);
        command.Parameters.AddWithValue("phone", "5551234567");
        command.Parameters.AddWithValue("tckn", "12345678901");
        command.Parameters.AddWithValue("password", "hashed_password_here");
        command.Parameters.AddWithValue("firstName", "Test");
        command.Parameters.AddWithValue("lastName", "User");
        command.Parameters.AddWithValue("professionId", 999999); // Invalid profession ID
        command.Parameters.AddWithValue("birthDate", new DateTime(1990, 1, 1));

        // This should throw an exception due to foreign key constraint
        var exception = await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await command.ExecuteNonQueryAsync();
        });

        exception.SqlState.Should().Be("23503"); // Foreign key violation
    }

    [Fact]
    public async Task Unique_Constraints_Should_Work()
    {
        // Arrange
        using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        var testEmail = $"unique_test_{Guid.NewGuid():N}@example.com";
        var testTckn = "98765432109";

        // Act 1: Insert first user
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                INSERT INTO users (email, username, phone_number, tckn, password_hash, 
                                 first_name, last_name, profession_id, birth_date)
                VALUES (@email, @username, @phone, @tckn, @password, 
                        @firstName, @lastName, @professionId, @birthDate);";

            command.Parameters.AddWithValue("email", testEmail);
            command.Parameters.AddWithValue("username", $"unique_user_{Guid.NewGuid():N}"[..20]);
            command.Parameters.AddWithValue("phone", "5551111111");
            command.Parameters.AddWithValue("tckn", testTckn);
            command.Parameters.AddWithValue("password", "hashed_password_here");
            command.Parameters.AddWithValue("firstName", "Unique");
            command.Parameters.AddWithValue("lastName", "User");
            command.Parameters.AddWithValue("professionId", 1);
            command.Parameters.AddWithValue("birthDate", new DateTime(1990, 1, 1));

            await command.ExecuteNonQueryAsync();
        }

        // Act 2: Try to insert another user with same email
        using var duplicateCommand = connection.CreateCommand();
        duplicateCommand.CommandText = @"
            INSERT INTO users (email, username, phone_number, tckn, password_hash, 
                             first_name, last_name, profession_id, birth_date)
            VALUES (@email, @username, @phone, @tckn, @password, 
                    @firstName, @lastName, @professionId, @birthDate);";

        duplicateCommand.Parameters.AddWithValue("email", testEmail); // Same email
        duplicateCommand.Parameters.AddWithValue("username", $"different_user_{Guid.NewGuid():N}"[..20]);
        duplicateCommand.Parameters.AddWithValue("phone", "5552222222");
        duplicateCommand.Parameters.AddWithValue("tckn", "11111111111"); // Different TCKN
        duplicateCommand.Parameters.AddWithValue("password", "hashed_password_here");
        duplicateCommand.Parameters.AddWithValue("firstName", "Different");
        duplicateCommand.Parameters.AddWithValue("lastName", "User");
        duplicateCommand.Parameters.AddWithValue("professionId", 1);
        duplicateCommand.Parameters.AddWithValue("birthDate", new DateTime(1990, 1, 1));

        // Assert: Should throw unique constraint violation
        var exception = await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await duplicateCommand.ExecuteNonQueryAsync();
        });

        exception.SqlState.Should().Be("23505"); // Unique violation
    }
} 