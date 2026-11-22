using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BCSStudyHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscoveredContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscoveredContents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    SourceId = table.Column<Guid>(type: "char(36)", nullable: false),
                    CanonicalUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    UrlHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Language = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true),
                    Kind = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    Excerpt = table.Column<string>(type: "varchar(600)", maxLength: 600, nullable: true),
                    TextContent = table.Column<string>(type: "longtext", nullable: true),
                    ContentHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    ETag = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    LastModifiedHeader = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    Visibility = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    DuplicateOfId = table.Column<Guid>(type: "char(36)", nullable: true),
                    DiscoveredAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscoveredContents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscoveredContents_ContentSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ContentSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DiscoveredContentVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    ContentId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Excerpt = table.Column<string>(type: "varchar(600)", maxLength: 600, nullable: true),
                    TextContent = table.Column<string>(type: "longtext", nullable: true),
                    ContentHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ScanId = table.Column<Guid>(type: "char(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscoveredContentVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscoveredContentVersions_DiscoveredContents_ContentId",
                        column: x => x.ContentId,
                        principalTable: "DiscoveredContents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveredContents_ContentHash",
                table: "DiscoveredContents",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveredContents_SourceId_UrlHash",
                table: "DiscoveredContents",
                columns: new[] { "SourceId", "UrlHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveredContents_Status_DiscoveredAt",
                table: "DiscoveredContents",
                columns: new[] { "Status", "DiscoveredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveredContentVersions_ContentId_Version",
                table: "DiscoveredContentVersions",
                columns: new[] { "ContentId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscoveredContentVersions");

            migrationBuilder.DropTable(
                name: "DiscoveredContents");
        }
    }
}
