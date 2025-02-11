using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwipeVortexWb.Migrations.InstagramDb
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TopPostsAnalyses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Hashtag = table.Column<string>(type: "TEXT", nullable: false),
                    AnalysisDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TopMediaDataIds = table.Column<string>(type: "TEXT", nullable: false),
                    AnalysisRank = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageFinalScore = table.Column<double>(type: "REAL", nullable: false),
                    TotalImpressions = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalLikes = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopPostsAnalyses", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TopPostsAnalyses");
        }
    }
}
