using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    public partial class AddedDogGroupToDogBreed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                schema: "Dsc",
                table: "DogBreeds",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DogBreeds_GroupId",
                schema: "Dsc",
                table: "DogBreeds",
                column: "GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_DogBreeds_DogGroups_GroupId",
                schema: "Dsc",
                table: "DogBreeds",
                column: "GroupId",
                principalSchema: "Dsc",
                principalTable: "DogGroups",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DogBreeds_DogGroups_GroupId",
                schema: "Dsc",
                table: "DogBreeds");

            migrationBuilder.DropIndex(
                name: "IX_DogBreeds_GroupId",
                schema: "Dsc",
                table: "DogBreeds");

            migrationBuilder.DropColumn(
                name: "GroupId",
                schema: "Dsc",
                table: "DogBreeds");
        }
    }
}
