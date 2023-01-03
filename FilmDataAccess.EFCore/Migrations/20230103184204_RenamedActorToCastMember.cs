using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmDataAccess.EFCore.Migrations
{
    public partial class RenamedActorToCastMember : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CastMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExternalId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CastMembers", x => x.Id);
                    table.UniqueConstraint("AK_CastMembers_ExternalId", x => x.ExternalId);
                });

            migrationBuilder.CreateTable(
                name: "CastMemberMovie",
                columns: table => new
                {
                    CastMembersId = table.Column<int>(type: "INTEGER", nullable: false),
                    MoviesId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CastMemberMovie", x => new { x.CastMembersId, x.MoviesId });
                    table.ForeignKey(
                        name: "FK_CastMemberMovie_CastMembers_CastMembersId",
                        column: x => x.CastMembersId,
                        principalTable: "CastMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CastMemberMovie_Movies_MoviesId",
                        column: x => x.MoviesId,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CastMemberMovie_MoviesId",
                table: "CastMemberMovie",
                column: "MoviesId");

            // ------------
            // CUSTOM STEP

            // migrating the previously existing data into thew new tables
            //   - Actors -> CastMembers (all the known cast members)
            //   - ActorMovie -> CastMemberMovie (all the known cast members for every movie)
            migrationBuilder.Sql("insert into CastMembers(Id, ExternalId, Name) select Id, ExternalId, Name from Actors;");
            migrationBuilder.Sql("insert into CastMemberMovie(CastMembersId, MoviesId) select ActorsId, MoviesId from ActorMovie;");

            // this step was originally in the beginning of the method
            migrationBuilder.DropTable(
                name: "ActorMovie");

            migrationBuilder.DropTable(
                name: "Actors");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Actors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExternalId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actors", x => x.Id);
                    table.UniqueConstraint("AK_Actors_ExternalId", x => x.ExternalId);
                });

            migrationBuilder.CreateTable(
                name: "ActorMovie",
                columns: table => new
                {
                    ActorsId = table.Column<int>(type: "INTEGER", nullable: false),
                    MoviesId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActorMovie", x => new { x.ActorsId, x.MoviesId });
                    table.ForeignKey(
                        name: "FK_ActorMovie_Actors_ActorsId",
                        column: x => x.ActorsId,
                        principalTable: "Actors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActorMovie_Movies_MoviesId",
                        column: x => x.MoviesId,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActorMovie_MoviesId",
                table: "ActorMovie",
                column: "MoviesId");

            // ------------
            // CUSTOM STEP

            // inverse operations of the CUSTOM STEP in the Up method 

            migrationBuilder.Sql("insert into Actors(Id, ExternalId, Name) select Id, ExternalId, Name from CastMembers;");
            migrationBuilder.Sql("insert into ActorMovie(ActorsId, MoviesId) select CastMembersId, MoviesId from CastMemberMovie;");

            migrationBuilder.DropTable(
                name: "CastMemberMovie");

            migrationBuilder.DropTable(
                name: "CastMembers");
        }
    }
}
