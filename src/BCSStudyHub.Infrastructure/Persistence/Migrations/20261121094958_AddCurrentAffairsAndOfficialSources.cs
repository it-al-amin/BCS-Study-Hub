using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BCSStudyHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentAffairsAndOfficialSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OfficialSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    LocalizedName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    Category = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    Url = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    Description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                    ParentOrganization = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    IsInternational = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UsageNotes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                    VerificationStatus = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    LastVerifiedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    VerifiedById = table.Column<Guid>(type: "char(36)", nullable: true),
                    VerificationNote = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficialSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfficialSources_AspNetUsers_VerifiedById",
                        column: x => x.VerifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CurrentAffairsEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    Summary = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: true),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SourcePublishedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Category = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    SourceName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    SourceUrl = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false),
                    OfficialSourceId = table.Column<Guid>(type: "char(36)", nullable: true),
                    SourceStatus = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    Notes = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true),
                    CreatedById = table.Column<Guid>(type: "char(36)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentAffairsEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurrentAffairsEntries_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurrentAffairsEntries_OfficialSources_OfficialSourceId",
                        column: x => x.OfficialSourceId,
                        principalTable: "OfficialSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OfficialSourceTags",
                columns: table => new
                {
                    SourceId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Tag = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficialSourceTags", x => new { x.SourceId, x.Tag });
                    table.ForeignKey(
                        name: "FK_OfficialSourceTags_OfficialSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "OfficialSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CurrentAffairsBookmarks",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    EntryId = table.Column<Guid>(type: "char(36)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentAffairsBookmarks", x => new { x.UserId, x.EntryId });
                    table.ForeignKey(
                        name: "FK_CurrentAffairsBookmarks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CurrentAffairsBookmarks_CurrentAffairsEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "CurrentAffairsEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CurrentAffairsEntryQuestions",
                columns: table => new
                {
                    EntryId = table.Column<Guid>(type: "char(36)", nullable: false),
                    QuestionType = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    QuestionId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentAffairsEntryQuestions", x => new { x.EntryId, x.QuestionType, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_CurrentAffairsEntryQuestions_CurrentAffairsEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "CurrentAffairsEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CurrentAffairsEntryReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    EntryId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Kind = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    Title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    Url = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentAffairsEntryReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurrentAffairsEntryReferences_CurrentAffairsEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "CurrentAffairsEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CurrentAffairsEntryResources",
                columns: table => new
                {
                    EntryId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ResourceId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentAffairsEntryResources", x => new { x.EntryId, x.ResourceId });
                    table.ForeignKey(
                        name: "FK_CurrentAffairsEntryResources_CurrentAffairsEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "CurrentAffairsEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CurrentAffairsEntryResources_LibraryResources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "LibraryResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CurrentAffairsEntrySubjects",
                columns: table => new
                {
                    EntryId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SubjectId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentAffairsEntrySubjects", x => new { x.EntryId, x.SubjectId });
                    table.ForeignKey(
                        name: "FK_CurrentAffairsEntrySubjects_CurrentAffairsEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "CurrentAffairsEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CurrentAffairsEntrySubjects_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CurrentAffairsEntryTags",
                columns: table => new
                {
                    EntryId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Tag = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentAffairsEntryTags", x => new { x.EntryId, x.Tag });
                    table.ForeignKey(
                        name: "FK_CurrentAffairsEntryTags_CurrentAffairsEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "CurrentAffairsEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CurrentAffairsEntryTopics",
                columns: table => new
                {
                    EntryId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SyllabusItemId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentAffairsEntryTopics", x => new { x.EntryId, x.SyllabusItemId });
                    table.ForeignKey(
                        name: "FK_CurrentAffairsEntryTopics_CurrentAffairsEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "CurrentAffairsEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CurrentAffairsEntryTopics_SyllabusItems_SyllabusItemId",
                        column: x => x.SyllabusItemId,
                        principalTable: "SyllabusItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CurrentAffairsRevisions",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    EntryId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ReviewCount = table.Column<int>(type: "int", nullable: false),
                    IntervalDays = table.Column<int>(type: "int", nullable: false),
                    NextReviewOn = table.Column<DateOnly>(type: "date", nullable: false),
                    LastReviewedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    LastOutcome = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true),
                    AddedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentAffairsRevisions", x => new { x.UserId, x.EntryId });
                    table.ForeignKey(
                        name: "FK_CurrentAffairsRevisions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CurrentAffairsRevisions_CurrentAffairsEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "CurrentAffairsEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CurrentAffairsStudyNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    EntryId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentAffairsStudyNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurrentAffairsStudyNotes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CurrentAffairsStudyNotes_CurrentAffairsEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "CurrentAffairsEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentAffairsBookmarks_EntryId",
                table: "CurrentAffairsBookmarks",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentAffairsBookmarks_UserId_CreatedAt",
                table: "CurrentAffairsBookmarks",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CurrentAffairsEntries_CreatedById",
                table: "CurrentAffairsEntries",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentAffairsEntries_OfficialSourceId",
                table: "CurrentAffairsEntries",
                column: "OfficialSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentAffairsEntries_Status_Category_EventDate",
                table: "CurrentAffairsEntries",
                columns: new[] { "Status", "Category", "EventDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CurrentAffairsEntries_Status_EventDate",
                table: "CurrentAffairsEntries",
                columns: new[] { "Status", "EventDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CurrentAffairsEntries_Title",
                table: "CurrentAffairsEntries",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentAffairsEntryQuestions_QuestionType_QuestionId",
                table: "CurrentAffairsEntryQuestions",
                columns: new[] { "QuestionType", "QuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_CurrentAffairsEntryReferences_EntryId",
                table: "CurrentAffairsEntryReferences",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentAffairsEntryResources_ResourceId",
                table: "CurrentAffairsEntryResources",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentAffairsEntrySubjects_SubjectId",
                table: "CurrentAffairsEntrySubjects",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentAffairsEntryTags_Tag",
                table: "CurrentAffairsEntryTags",
                column: "Tag");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentAffairsEntryTopics_SyllabusItemId",
                table: "CurrentAffairsEntryTopics",
                column: "SyllabusItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentAffairsRevisions_EntryId",
                table: "CurrentAffairsRevisions",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentAffairsRevisions_UserId_NextReviewOn",
                table: "CurrentAffairsRevisions",
                columns: new[] { "UserId", "NextReviewOn" });

            migrationBuilder.CreateIndex(
                name: "IX_CurrentAffairsStudyNotes_EntryId",
                table: "CurrentAffairsStudyNotes",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentAffairsStudyNotes_UserId_EntryId",
                table: "CurrentAffairsStudyNotes",
                columns: new[] { "UserId", "EntryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurrentAffairsStudyNotes_UserId_UpdatedAt",
                table: "CurrentAffairsStudyNotes",
                columns: new[] { "UserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OfficialSources_Status_Category_SortOrder",
                table: "OfficialSources",
                columns: new[] { "Status", "Category", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_OfficialSources_Url",
                table: "OfficialSources",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfficialSources_VerifiedById",
                table: "OfficialSources",
                column: "VerifiedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurrentAffairsBookmarks");

            migrationBuilder.DropTable(
                name: "CurrentAffairsEntryQuestions");

            migrationBuilder.DropTable(
                name: "CurrentAffairsEntryReferences");

            migrationBuilder.DropTable(
                name: "CurrentAffairsEntryResources");

            migrationBuilder.DropTable(
                name: "CurrentAffairsEntrySubjects");

            migrationBuilder.DropTable(
                name: "CurrentAffairsEntryTags");

            migrationBuilder.DropTable(
                name: "CurrentAffairsEntryTopics");

            migrationBuilder.DropTable(
                name: "CurrentAffairsRevisions");

            migrationBuilder.DropTable(
                name: "CurrentAffairsStudyNotes");

            migrationBuilder.DropTable(
                name: "OfficialSourceTags");

            migrationBuilder.DropTable(
                name: "CurrentAffairsEntries");

            migrationBuilder.DropTable(
                name: "OfficialSources");
        }
    }
}
