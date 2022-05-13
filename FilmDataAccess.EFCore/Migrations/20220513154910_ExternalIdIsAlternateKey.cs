using Microsoft.EntityFrameworkCore.Migrations;

namespace FilmDataAccess.EFCore.Migrations
{
    public partial class ExternalIdIsAlternateKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AK_Directors_Name",
                table: "Directors");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Actors_Name",
                table: "Actors");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Directors_ExternalId",
                table: "Directors",
                column: "ExternalId");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Actors_ExternalId",
                table: "Actors",
                column: "ExternalId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AK_Directors_ExternalId",
                table: "Directors");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Actors_ExternalId",
                table: "Actors");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Directors_Name",
                table: "Directors",
                column: "Name");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Actors_Name",
                table: "Actors",
                column: "Name");
        }
    }
}
