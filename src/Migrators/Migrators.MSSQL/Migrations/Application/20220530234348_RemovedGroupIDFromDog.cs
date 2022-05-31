using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    public partial class RemovedGroupIDFromDog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dogs_DogGroups_GroupId",
                schema: "Dsc",
                table: "Dogs");

            migrationBuilder.DropIndex(
                name: "IX_Dogs_GroupId",
                schema: "Dsc",
                table: "Dogs");

            migrationBuilder.DropColumn(
                name: "GroupId",
                schema: "Dsc",
                table: "Dogs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                schema: "Dsc",
                table: "Dogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dogs_GroupId",
                schema: "Dsc",
                table: "Dogs",
                column: "GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Dogs_DogGroups_GroupId",
                schema: "Dsc",
                table: "Dogs",
                column: "GroupId",
                principalSchema: "Dsc",
                principalTable: "DogGroups",
                principalColumn: "Id");
        }
    }
}
