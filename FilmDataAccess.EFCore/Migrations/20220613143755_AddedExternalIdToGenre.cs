using Microsoft.EntityFrameworkCore.Migrations;

namespace FilmDataAccess.EFCore.Migrations
{
    public partial class AddedExternalIdToGenre : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AK_Genres_Name",
                table: "Genres");

            migrationBuilder.AddColumn<int>(
                name: "ExternalId",
                table: "Genres",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Genres_ExternalId",
                table: "Genres",
                column: "ExternalId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AK_Genres_ExternalId",
                table: "Genres");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Genres");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Genres_Name",
                table: "Genres",
                column: "Name");
        }
    }
}
