using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    public partial class DogBreedToDogTraitRelationship : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DogTraits_DogBreeds_DogBreedId",
                schema: "Dsc",
                table: "DogTraits");

            migrationBuilder.DropIndex(
                name: "IX_DogTraits_DogBreedId",
                schema: "Dsc",
                table: "DogTraits");

            migrationBuilder.DropColumn(
                name: "DogBreedId",
                schema: "Dsc",
                table: "DogTraits");

            migrationBuilder.CreateTable(
                name: "DogBreedDogTrait",
                schema: "Dsc",
                columns: table => new
                {
                    BreedsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TraitsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DogBreedDogTrait", x => new { x.BreedsId, x.TraitsId });
                    table.ForeignKey(
                        name: "FK_DogBreedDogTrait_DogBreeds_BreedsId",
                        column: x => x.BreedsId,
                        principalSchema: "Dsc",
                        principalTable: "DogBreeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DogBreedDogTrait_DogTraits_TraitsId",
                        column: x => x.TraitsId,
                        principalSchema: "Dsc",
                        principalTable: "DogTraits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DogBreedDogTrait_TraitsId",
                schema: "Dsc",
                table: "DogBreedDogTrait",
                column: "TraitsId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DogBreedDogTrait",
                schema: "Dsc");

            migrationBuilder.AddColumn<Guid>(
                name: "DogBreedId",
                schema: "Dsc",
                table: "DogTraits",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DogTraits_DogBreedId",
                schema: "Dsc",
                table: "DogTraits",
                column: "DogBreedId");

            migrationBuilder.AddForeignKey(
                name: "FK_DogTraits_DogBreeds_DogBreedId",
                schema: "Dsc",
                table: "DogTraits",
                column: "DogBreedId",
                principalSchema: "Dsc",
                principalTable: "DogBreeds",
                principalColumn: "Id");
        }
    }
}
