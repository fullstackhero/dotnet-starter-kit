using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    public partial class AddedDogEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Dsc");

            migrationBuilder.CreateTable(
                name: "DogBreeds",
                schema: "Dsc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    About = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DogBreeds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DogGroups",
                schema: "Dsc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DogGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DogColors",
                schema: "Dsc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsStandard = table.Column<bool>(type: "bit", nullable: true),
                    RegistrationCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DogBreedId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DogColors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DogColors_DogBreeds_DogBreedId",
                        column: x => x.DogBreedId,
                        principalSchema: "Dsc",
                        principalTable: "DogBreeds",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DogTraits",
                schema: "Dsc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DogBreedId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DogTraits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DogTraits_DogBreeds_DogBreedId",
                        column: x => x.DogBreedId,
                        principalSchema: "Dsc",
                        principalTable: "DogBreeds",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Dogs",
                schema: "Dsc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OfficialName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AkcId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Birthdate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BreedId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Microchip = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ColorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dogs_DogBreeds_BreedId",
                        column: x => x.BreedId,
                        principalSchema: "Dsc",
                        principalTable: "DogBreeds",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Dogs_DogColors_ColorId",
                        column: x => x.ColorId,
                        principalSchema: "Dsc",
                        principalTable: "DogColors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Dogs_DogGroups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "Dsc",
                        principalTable: "DogGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DogColors_DogBreedId",
                schema: "Dsc",
                table: "DogColors",
                column: "DogBreedId");

            migrationBuilder.CreateIndex(
                name: "IX_Dogs_BreedId",
                schema: "Dsc",
                table: "Dogs",
                column: "BreedId");

            migrationBuilder.CreateIndex(
                name: "IX_Dogs_ColorId",
                schema: "Dsc",
                table: "Dogs",
                column: "ColorId");

            migrationBuilder.CreateIndex(
                name: "IX_Dogs_GroupId",
                schema: "Dsc",
                table: "Dogs",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_DogTraits_DogBreedId",
                schema: "Dsc",
                table: "DogTraits",
                column: "DogBreedId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dogs",
                schema: "Dsc");

            migrationBuilder.DropTable(
                name: "DogTraits",
                schema: "Dsc");

            migrationBuilder.DropTable(
                name: "DogColors",
                schema: "Dsc");

            migrationBuilder.DropTable(
                name: "DogGroups",
                schema: "Dsc");

            migrationBuilder.DropTable(
                name: "DogBreeds",
                schema: "Dsc");
        }
    }
}
