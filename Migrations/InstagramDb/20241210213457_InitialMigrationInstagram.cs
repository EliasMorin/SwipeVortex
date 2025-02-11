using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwipeVortexWb.Migrations.InstagramDb
{
    /// <inheritdoc />
    public partial class InitialMigrationInstagram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HashtagAnalyses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Hashtag = table.Column<string>(type: "TEXT", nullable: false),
                    ScrapeDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalMediaCount = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HashtagAnalyses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TotalHashtagsAnalyzed = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalPostsProcessed = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalUniqueUsers = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalRelatedHashtags = table.Column<long>(type: "INTEGER", nullable: false),
                    LastAnalysisDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserName = table.Column<string>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    FollowerCount = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RelatedHashtags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    MediaCount = table.Column<long>(type: "INTEGER", nullable: false),
                    InstagramHashtagAnalysisId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelatedHashtags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RelatedHashtags_HashtagAnalyses_InstagramHashtagAnalysisId",
                        column: x => x.InstagramHashtagAnalysisId,
                        principalTable: "HashtagAnalyses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Medias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true),
                    MediaCode = table.Column<string>(type: "TEXT", nullable: false),
                    MediaType = table.Column<string>(type: "TEXT", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LikesCount = table.Column<long>(type: "INTEGER", nullable: false),
                    CommentsCount = table.Column<long>(type: "INTEGER", nullable: false),
                    Caption = table.Column<string>(type: "TEXT", nullable: false),
                    TopicRelevance = table.Column<double>(type: "REAL", nullable: false),
                    PenetrationRate = table.Column<double>(type: "REAL", nullable: false),
                    FinalScore = table.Column<double>(type: "REAL", nullable: false),
                    PostUrl = table.Column<string>(type: "TEXT", nullable: false),
                    InstagramHashtagAnalysisId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Medias_HashtagAnalyses_InstagramHashtagAnalysisId",
                        column: x => x.InstagramHashtagAnalysisId,
                        principalTable: "HashtagAnalyses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Medias_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CategoryScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CategoryName = table.Column<string>(type: "TEXT", nullable: false),
                    Score = table.Column<double>(type: "REAL", nullable: false),
                    MediaDataId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryScores_Medias_MediaDataId",
                        column: x => x.MediaDataId,
                        principalTable: "Medias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryScores_MediaDataId",
                table: "CategoryScores",
                column: "MediaDataId");

            migrationBuilder.CreateIndex(
                name: "IX_Medias_InstagramHashtagAnalysisId",
                table: "Medias",
                column: "InstagramHashtagAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_Medias_UserId",
                table: "Medias",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RelatedHashtags_InstagramHashtagAnalysisId",
                table: "RelatedHashtags",
                column: "InstagramHashtagAnalysisId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryScores");

            migrationBuilder.DropTable(
                name: "RelatedHashtags");

            migrationBuilder.DropTable(
                name: "Stats");

            migrationBuilder.DropTable(
                name: "Medias");

            migrationBuilder.DropTable(
                name: "HashtagAnalyses");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
