using Microsoft.EntityFrameworkCore.Migrations;

namespace FilmDataAccess.EFCore.Migrations
{
    public partial class AddedOriginalTitle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OriginalTitle",
                table: "Movies",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalTitle",
                table: "Movies");
        }
    }
}
