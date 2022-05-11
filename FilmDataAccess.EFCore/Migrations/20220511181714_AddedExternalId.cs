using Microsoft.EntityFrameworkCore.Migrations;

namespace FilmDataAccess.EFCore.Migrations
{
    public partial class AddedExternalId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExternalId",
                table: "Movies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExternalId",
                table: "Genres",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExternalId",
                table: "Directors",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExternalId",
                table: "Actors",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Genres");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Directors");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Actors");
        }
    }
}
