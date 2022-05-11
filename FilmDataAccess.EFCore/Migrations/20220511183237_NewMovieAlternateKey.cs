﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace FilmDataAccess.EFCore.Migrations
{
    public partial class NewMovieAlternateKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AK_Movies_Title_ReleaseDate",
                table: "Movies");

            migrationBuilder.AlterColumn<string>(
                name: "OriginalTitle",
                table: "Movies",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Movies_Title_OriginalTitle_ReleaseDate",
                table: "Movies",
                columns: new[] { "Title", "OriginalTitle", "ReleaseDate" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AK_Movies_Title_OriginalTitle_ReleaseDate",
                table: "Movies");

            migrationBuilder.AlterColumn<string>(
                name: "OriginalTitle",
                table: "Movies",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Movies_Title_ReleaseDate",
                table: "Movies",
                columns: new[] { "Title", "ReleaseDate" });
        }
    }
}
