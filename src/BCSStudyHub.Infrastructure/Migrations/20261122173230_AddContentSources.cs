using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BCSStudyHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContentSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    LocalizedName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    SourceType = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    ExtractionMethod = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    BaseUrl = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    FeedUrl = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                    SitemapUrl = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                    Category = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    Language = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Reliability = table.Column<int>(type: "int", nullable: false),
                    ScanIntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    RetentionPolicy = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    IncludePatterns = table.Column<string>(type: "longtext", maxLength: 2000, nullable: true),
                    ExcludePatterns = table.Column<string>(type: "longtext", maxLength: 2000, nullable: true),
                    RequiresVerification = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RightsNote = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "longtext", maxLength: 2000, nullable: true),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    Health = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    LastScanAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    LastSuccessAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    LastError = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "int", nullable: false),
                    CreatedById = table.Column<Guid>(type: "char(36)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentSources", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ContentSourceScans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    SourceId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Trigger = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    RequestedById = table.Column<Guid>(type: "char(36)", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    DiscoveredCount = table.Column<int>(type: "int", nullable: false),
                    UpdatedCount = table.Column<int>(type: "int", nullable: false),
                    UnchangedCount = table.Column<int>(type: "int", nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentSourceScans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentSourceScans_ContentSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ContentSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ContentSources_BaseUrl",
                table: "ContentSources",
                column: "BaseUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentSources_Category",
                table: "ContentSources",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ContentSources_Status_IsEnabled_Priority",
                table: "ContentSources",
                columns: new[] { "Status", "IsEnabled", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentSourceScans_SourceId_RequestedAt",
                table: "ContentSourceScans",
                columns: new[] { "SourceId", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentSourceScans_Status_RequestedAt",
                table: "ContentSourceScans",
                columns: new[] { "Status", "RequestedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentSourceScans");

            migrationBuilder.DropTable(
                name: "ContentSources");
        }
    }
}
