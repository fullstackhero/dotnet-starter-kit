using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Identity
{
    /// <inheritdoc />
    public partial class AddRoleClaimsUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove the duplicate grants concurrent permission writers left behind, keeping the lowest Id
            // per (RoleId, ClaimType, ClaimValue). Must run before the unique index below is created.
            // Plain equality matches the index: rows with a NULL ClaimType or ClaimValue never conflict.
            migrationBuilder.Sql(
                """
                DELETE FROM "identity"."RoleClaims" AS dup
                USING "identity"."RoleClaims" AS kept
                WHERE dup."RoleId" = kept."RoleId"
                  AND dup."ClaimType" = kept."ClaimType"
                  AND dup."ClaimValue" = kept."ClaimValue"
                  AND dup."Id" > kept."Id";
                """);

            migrationBuilder.DropIndex(
                name: "IX_RoleClaims_RoleId",
                schema: "identity",
                table: "RoleClaims");

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId_ClaimType_ClaimValue",
                schema: "identity",
                table: "RoleClaims",
                columns: new[] { "RoleId", "ClaimType", "ClaimValue" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoleClaims_RoleId_ClaimType_ClaimValue",
                schema: "identity",
                table: "RoleClaims");

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                schema: "identity",
                table: "RoleClaims",
                column: "RoleId");
        }
    }
}
