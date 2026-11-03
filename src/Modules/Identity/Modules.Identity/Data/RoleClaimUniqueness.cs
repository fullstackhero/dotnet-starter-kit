using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FSH.Modules.Identity.Data;

/// <summary>
/// The unique index on <c>identity.RoleClaims (RoleId, ClaimType, ClaimValue)</c> and how the permission
/// writers recognise a conflict on it: when two writers (two instances starting at once, or the seeder and
/// the startup sync) insert the same grant, the second insert is rejected and the row already exists.
/// </summary>
internal static partial class RoleClaimUniqueness
{
    internal const string IndexName = "IX_RoleClaims_RoleId_ClaimType_ClaimValue";

    /// <summary>True when the save failed only because another writer already inserted the same role claim.</summary>
    internal static bool IsDuplicateRoleClaim(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: IndexName,
        };

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Permission claims for '{Role}' in tenant '{Tenant}' were already inserted by another writer; skipping")]
    internal static partial void LogRoleClaimsAlreadyInserted(this ILogger logger, string? role, string? tenant);
}
