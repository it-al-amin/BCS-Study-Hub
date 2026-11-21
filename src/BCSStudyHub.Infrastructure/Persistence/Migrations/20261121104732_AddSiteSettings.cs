using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BCSStudyHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    SiteName = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Tagline = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    AnnouncementEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AnnouncementText = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    AnnouncementTone = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    AnnouncementStartsAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    AnnouncementEndsAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    SupportEmail = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: true),
                    RegistrationOpen = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FooterNotice = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    UpdatedById = table.Column<Guid>(type: "char(36)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteSettings");
        }
    }
}
