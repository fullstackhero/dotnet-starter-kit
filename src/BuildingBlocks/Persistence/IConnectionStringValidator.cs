namespace FSH.Framework.Persistence;

/// <summary>
/// Interface for validating database connection strings.
/// </summary>
public interface IConnectionStringValidator
{
    /// <summary>
    /// Validates the specified connection string format and accessibility.
    /// </summary>
    /// <param name="connectionString">The connection string to validate.</param>
    /// <param name="dbProvider">Optional database provider type for provider-specific validation.</param>
    /// <returns>true if the connection string is valid; otherwise, false.</returns>
    bool TryValidate(string connectionString, string? dbProvider = null);
}