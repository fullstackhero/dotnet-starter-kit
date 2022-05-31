using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    public partial class AddedDogCollectionToBreed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dogs_DogBreeds_BreedId",
                schema: "Dsc",
                table: "Dogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Dogs_DogColors_ColorId",
                schema: "Dsc",
                table: "Dogs");

            migrationBuilder.RenameColumn(
                name: "ColorId",
                schema: "Dsc",
                table: "Dogs",
                newName: "DogColorId");

            migrationBuilder.RenameColumn(
                name: "BreedId",
                schema: "Dsc",
                table: "Dogs",
                newName: "DogBreedId");

            migrationBuilder.RenameIndex(
                name: "IX_Dogs_ColorId",
                schema: "Dsc",
                table: "Dogs",
                newName: "IX_Dogs_DogColorId");

            migrationBuilder.RenameIndex(
                name: "IX_Dogs_BreedId",
                schema: "Dsc",
                table: "Dogs",
                newName: "IX_Dogs_DogBreedId");

            migrationBuilder.AddForeignKey(
                name: "FK_Dogs_DogBreeds_DogBreedId",
                schema: "Dsc",
                table: "Dogs",
                column: "DogBreedId",
                principalSchema: "Dsc",
                principalTable: "DogBreeds",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Dogs_DogColors_DogColorId",
                schema: "Dsc",
                table: "Dogs",
                column: "DogColorId",
                principalSchema: "Dsc",
                principalTable: "DogColors",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dogs_DogBreeds_DogBreedId",
                schema: "Dsc",
                table: "Dogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Dogs_DogColors_DogColorId",
                schema: "Dsc",
                table: "Dogs");

            migrationBuilder.RenameColumn(
                name: "DogColorId",
                schema: "Dsc",
                table: "Dogs",
                newName: "ColorId");

            migrationBuilder.RenameColumn(
                name: "DogBreedId",
                schema: "Dsc",
                table: "Dogs",
                newName: "BreedId");

            migrationBuilder.RenameIndex(
                name: "IX_Dogs_DogColorId",
                schema: "Dsc",
                table: "Dogs",
                newName: "IX_Dogs_ColorId");

            migrationBuilder.RenameIndex(
                name: "IX_Dogs_DogBreedId",
                schema: "Dsc",
                table: "Dogs",
                newName: "IX_Dogs_BreedId");

            migrationBuilder.AddForeignKey(
                name: "FK_Dogs_DogBreeds_BreedId",
                schema: "Dsc",
                table: "Dogs",
                column: "BreedId",
                principalSchema: "Dsc",
                principalTable: "DogBreeds",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Dogs_DogColors_ColorId",
                schema: "Dsc",
                table: "Dogs",
                column: "ColorId",
                principalSchema: "Dsc",
                principalTable: "DogColors",
                principalColumn: "Id");
        }
    }
}
