using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Identity
{
    /// <inheritdoc />
    public partial class UserGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Groups",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystemGroup = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GroupRoles",
                schema: "identity",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupRoles", x => new { x.GroupId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_GroupRoles_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "identity",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserGroups",
                schema: "identity",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    AddedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => new { x.UserId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_UserGroups_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "identity",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserGroups_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupRoles_GroupId",
                schema: "identity",
                table: "GroupRoles",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupRoles_RoleId",
                schema: "identity",
                table: "GroupRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_IsDefault",
                schema: "identity",
                table: "Groups",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_IsDeleted",
                schema: "identity",
                table: "Groups",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_Name",
                schema: "identity",
                table: "Groups",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_GroupId",
                schema: "identity",
                table: "UserGroups",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_UserId",
                schema: "identity",
                table: "UserGroups",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupRoles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserGroups",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Groups",
                schema: "identity");
        }
    }
}