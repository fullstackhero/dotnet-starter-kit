using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    public partial class DogColorRelationshipContraint2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DogColors_DogBreeds_DogBreedId",
                schema: "Dsc",
                table: "DogColors");

            migrationBuilder.DropIndex(
                name: "IX_DogColors_DogBreedId",
                schema: "Dsc",
                table: "DogColors");

            migrationBuilder.DropColumn(
                name: "DogBreedId",
                schema: "Dsc",
                table: "DogColors");

            migrationBuilder.CreateTable(
                name: "DogBreedDogColor",
                schema: "Dsc",
                columns: table => new
                {
                    BreedsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColorsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DogBreedDogColor", x => new { x.BreedsId, x.ColorsId });
                    table.ForeignKey(
                        name: "FK_DogBreedDogColor_DogBreeds_BreedsId",
                        column: x => x.BreedsId,
                        principalSchema: "Dsc",
                        principalTable: "DogBreeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DogBreedDogColor_DogColors_ColorsId",
                        column: x => x.ColorsId,
                        principalSchema: "Dsc",
                        principalTable: "DogColors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DogBreedDogColor_ColorsId",
                schema: "Dsc",
                table: "DogBreedDogColor",
                column: "ColorsId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DogBreedDogColor",
                schema: "Dsc");

            migrationBuilder.AddColumn<Guid>(
                name: "DogBreedId",
                schema: "Dsc",
                table: "DogColors",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DogColors_DogBreedId",
                schema: "Dsc",
                table: "DogColors",
                column: "DogBreedId");

            migrationBuilder.AddForeignKey(
                name: "FK_DogColors_DogBreeds_DogBreedId",
                schema: "Dsc",
                table: "DogColors",
                column: "DogBreedId",
                principalSchema: "Dsc",
                principalTable: "DogBreeds",
                principalColumn: "Id");
        }
    }
}
