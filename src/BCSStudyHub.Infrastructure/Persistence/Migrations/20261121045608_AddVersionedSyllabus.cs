using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BCSStudyHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionedSyllabus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamStages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    LocalizedName = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamStages", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Code = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "varchar(240)", maxLength: 240, nullable: false),
                    Description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SyllabusSourceReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    Publisher = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false),
                    Url = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    SourceType = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    PublishedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    LastVerifiedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    VerificationStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyllabusSourceReferences", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SyllabusVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    ExamStageId = table.Column<Guid>(type: "char(36)", nullable: false),
                    VersionLabel = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    TrackCode = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    Summary = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    Coverage = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyllabusVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyllabusVersions_ExamStages_ExamStageId",
                        column: x => x.ExamStageId,
                        principalTable: "ExamStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SyllabusSubjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    SyllabusVersionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SubjectId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Marks = table.Column<int>(type: "int", nullable: true),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyllabusSubjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyllabusSubjects_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyllabusSubjects_SyllabusVersions_SyllabusVersionId",
                        column: x => x.SyllabusVersionId,
                        principalTable: "SyllabusVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SyllabusVersionSources",
                columns: table => new
                {
                    SyllabusVersionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SourceReferenceId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Notes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyllabusVersionSources", x => new { x.SyllabusVersionId, x.SourceReferenceId });
                    table.ForeignKey(
                        name: "FK_SyllabusVersionSources_SyllabusSourceReferences_SourceRefere~",
                        column: x => x.SourceReferenceId,
                        principalTable: "SyllabusSourceReferences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyllabusVersionSources_SyllabusVersions_SyllabusVersionId",
                        column: x => x.SyllabusVersionId,
                        principalTable: "SyllabusVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Papers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    SyllabusSubjectId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Code = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "varchar(240)", maxLength: 240, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    MaxMarks = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Papers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Papers_SyllabusSubjects_SyllabusSubjectId",
                        column: x => x.SyllabusSubjectId,
                        principalTable: "SyllabusSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SyllabusItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    SyllabusSubjectId = table.Column<Guid>(type: "char(36)", nullable: false),
                    PaperId = table.Column<Guid>(type: "char(36)", nullable: true),
                    ParentId = table.Column<Guid>(type: "char(36)", nullable: true),
                    Kind = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    VerificationStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyllabusItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyllabusItems_Papers_PaperId",
                        column: x => x.PaperId,
                        principalTable: "Papers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyllabusItems_SyllabusItems_ParentId",
                        column: x => x.ParentId,
                        principalTable: "SyllabusItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyllabusItems_SyllabusSubjects_SyllabusSubjectId",
                        column: x => x.SyllabusSubjectId,
                        principalTable: "SyllabusSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SyllabusItemSources",
                columns: table => new
                {
                    SyllabusItemId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SourceReferenceId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyllabusItemSources", x => new { x.SyllabusItemId, x.SourceReferenceId });
                    table.ForeignKey(
                        name: "FK_SyllabusItemSources_SyllabusItems_SyllabusItemId",
                        column: x => x.SyllabusItemId,
                        principalTable: "SyllabusItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SyllabusItemSources_SyllabusSourceReferences_SourceReference~",
                        column: x => x.SourceReferenceId,
                        principalTable: "SyllabusSourceReferences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TopicProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SyllabusItemId = table.Column<Guid>(type: "char(36)", nullable: false),
                    IsCompleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopicProgress_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TopicProgress_SyllabusItems_SyllabusItemId",
                        column: x => x.SyllabusItemId,
                        principalTable: "SyllabusItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ExamStages_Code",
                table: "ExamStages",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Papers_SyllabusSubjectId_Code",
                table: "Papers",
                columns: new[] { "SyllabusSubjectId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_Code",
                table: "Subjects",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusItems_PaperId",
                table: "SyllabusItems",
                column: "PaperId");

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusItems_ParentId",
                table: "SyllabusItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusItems_SyllabusSubjectId_Kind_SortOrder",
                table: "SyllabusItems",
                columns: new[] { "SyllabusSubjectId", "Kind", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusItemSources_SourceReferenceId",
                table: "SyllabusItemSources",
                column: "SourceReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusSourceReferences_Url",
                table: "SyllabusSourceReferences",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusSubjects_SubjectId",
                table: "SyllabusSubjects",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusSubjects_SyllabusVersionId_SortOrder",
                table: "SyllabusSubjects",
                columns: new[] { "SyllabusVersionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusSubjects_SyllabusVersionId_SubjectId",
                table: "SyllabusSubjects",
                columns: new[] { "SyllabusVersionId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusVersions_ExamStageId_TrackCode_VersionLabel",
                table: "SyllabusVersions",
                columns: new[] { "ExamStageId", "TrackCode", "VersionLabel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusVersionSources_SourceReferenceId",
                table: "SyllabusVersionSources",
                column: "SourceReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicProgress_SyllabusItemId",
                table: "TopicProgress",
                column: "SyllabusItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicProgress_UserId_SyllabusItemId",
                table: "TopicProgress",
                columns: new[] { "UserId", "SyllabusItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyllabusItemSources");

            migrationBuilder.DropTable(
                name: "SyllabusVersionSources");

            migrationBuilder.DropTable(
                name: "TopicProgress");

            migrationBuilder.DropTable(
                name: "SyllabusSourceReferences");

            migrationBuilder.DropTable(
                name: "SyllabusItems");

            migrationBuilder.DropTable(
                name: "Papers");

            migrationBuilder.DropTable(
                name: "SyllabusSubjects");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropTable(
                name: "SyllabusVersions");

            migrationBuilder.DropTable(
                name: "ExamStages");
        }
    }
}
