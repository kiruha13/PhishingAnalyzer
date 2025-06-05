using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhishingAnalyzer.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalysisHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    AnalyzedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ScreenshotPath = table.Column<string>(type: "TEXT", nullable: true),
                    AnalysisResult = table.Column<string>(type: "TEXT", nullable: true),
                    RiskScore = table.Column<double>(type: "REAL", nullable: false),
                    IsPhishing = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisHistory_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisHistory_UserId",
                table: "AnalysisHistory",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysisHistory");
        }
    }
}
