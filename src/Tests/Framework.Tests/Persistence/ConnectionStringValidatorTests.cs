using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Framework.Tests.Persistence;

public sealed class ConnectionStringValidatorTests
{
    private static ConnectionStringValidator Build(string provider)
    {
        var options = Options.Create(new DatabaseOptions { Provider = provider });
        var logger = Substitute.For<ILogger<ConnectionStringValidator>>();
        return new ConnectionStringValidator(options, logger);
    }

    #region Happy Path

    [Fact]
    public void TryValidate_Should_ReturnTrue_When_PostgresConnectionStringValid()
    {
        // Arrange
        var sut = Build(DbProviders.PostgreSQL);

        // Act
        var result = sut.TryValidate("Host=localhost;Port=5432;Database=fsh;Username=postgres;Password=pwd");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void TryValidate_Should_ReturnTrue_When_MssqlConnectionStringValid()
    {
        // Arrange
        var sut = Build(DbProviders.MSSQL);

        // Act
        var result = sut.TryValidate("Server=localhost;Database=fsh;User Id=sa;Password=pwd;");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void TryValidate_Should_HonorExplicitProviderOverride_When_ProvidedArgument()
    {
        // Arrange — configured provider is Postgres, but call passes MSSQL explicitly.
        var sut = Build(DbProviders.PostgreSQL);

        // Act
        var result = sut.TryValidate("Server=localhost;Database=fsh;", DbProviders.MSSQL);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void TryValidate_Should_ReturnTrue_When_ProviderUnknown()
    {
        // Arrange — unknown provider falls through default arm without parsing.
        var sut = Build("SQLITE");

        // Act
        var result = sut.TryValidate("any-string");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void TryValidate_Should_ReturnFalse_When_PostgresConnectionStringMalformed()
    {
        // Arrange
        var sut = Build(DbProviders.PostgreSQL);

        // Act — unknown keyword triggers ArgumentException in the builder.
        var result = sut.TryValidate("Host=localhost;ThisKeyIsNotValid=oops");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void TryValidate_Should_ReturnTrue_When_EmptyOrWhitespace()
    {
        // Arrange — builders accept empty/whitespace as a valid (empty) connection string.
        var sut = Build(DbProviders.PostgreSQL);

        // Act & Assert
        sut.TryValidate(string.Empty).ShouldBeTrue();
        sut.TryValidate("   ").ShouldBeTrue();
    }

    #endregion
}
