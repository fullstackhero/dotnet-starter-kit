using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    public partial class BaseEntityFKFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DogBreeds_DogGroups_GroupId",
                schema: "Dsc",
                table: "DogBreeds");

            migrationBuilder.RenameColumn(
                name: "GroupId",
                schema: "Dsc",
                table: "DogBreeds",
                newName: "DogGroupId");

            migrationBuilder.RenameIndex(
                name: "IX_DogBreeds_GroupId",
                schema: "Dsc",
                table: "DogBreeds",
                newName: "IX_DogBreeds_DogGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_DogBreeds_DogGroups_DogGroupId",
                schema: "Dsc",
                table: "DogBreeds",
                column: "DogGroupId",
                principalSchema: "Dsc",
                principalTable: "DogGroups",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DogBreeds_DogGroups_DogGroupId",
                schema: "Dsc",
                table: "DogBreeds");

            migrationBuilder.RenameColumn(
                name: "DogGroupId",
                schema: "Dsc",
                table: "DogBreeds",
                newName: "GroupId");

            migrationBuilder.RenameIndex(
                name: "IX_DogBreeds_DogGroupId",
                schema: "Dsc",
                table: "DogBreeds",
                newName: "IX_DogBreeds_GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_DogBreeds_DogGroups_GroupId",
                schema: "Dsc",
                table: "DogBreeds",
                column: "GroupId",
                principalSchema: "Dsc",
                principalTable: "DogGroups",
                principalColumn: "Id");
        }
    }
}
