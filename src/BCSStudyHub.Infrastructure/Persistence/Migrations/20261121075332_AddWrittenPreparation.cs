using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BCSStudyHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWrittenPreparation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WrittenGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TargetSubmissions = table.Column<int>(type: "int", nullable: false),
                    TargetWords = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WrittenGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WrittenGoals_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WrittenQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    SyllabusItemId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Prompt = table.Column<string>(type: "varchar(12000)", maxLength: 12000, nullable: false),
                    Type = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    BcsExamYear = table.Column<int>(type: "int", nullable: true),
                    ExamNumber = table.Column<int>(type: "int", nullable: true),
                    ExamReference = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: true),
                    Marks = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    AnswerGuidance = table.Column<string>(type: "varchar(8000)", maxLength: 8000, nullable: true),
                    CreatedById = table.Column<Guid>(type: "char(36)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WrittenQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WrittenQuestions_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WrittenQuestions_SyllabusItems_SyllabusItemId",
                        column: x => x.SyllabusItemId,
                        principalTable: "SyllabusItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WrittenStudyNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SyllabusItemId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WrittenStudyNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WrittenStudyNotes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WrittenStudyNotes_SyllabusItems_SyllabusItemId",
                        column: x => x.SyllabusItemId,
                        principalTable: "SyllabusItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WrittenAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    QuestionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false),
                    WordCount = table.Column<int>(type: "int", nullable: false),
                    TimeSpentSeconds = table.Column<int>(type: "int", nullable: false),
                    SelfAssessmentJson = table.Column<string>(type: "longtext", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WrittenAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WrittenAnswers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WrittenAnswers_WrittenQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "WrittenQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WrittenBookmarks",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    QuestionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WrittenBookmarks", x => new { x.UserId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_WrittenBookmarks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WrittenBookmarks_WrittenQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "WrittenQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WrittenModelAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    QuestionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    UpdatedById = table.Column<Guid>(type: "char(36)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WrittenModelAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WrittenModelAnswers_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WrittenModelAnswers_WrittenQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "WrittenQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WrittenQuestionResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    QuestionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    Url = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WrittenQuestionResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WrittenQuestionResources_WrittenQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "WrittenQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WrittenQuestionSourceReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    QuestionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    Publisher = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false),
                    Url = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    Citation = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    PublishedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    VerificationStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WrittenQuestionSourceReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WrittenQuestionSourceReferences_WrittenQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "WrittenQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WrittenQuestionTags",
                columns: table => new
                {
                    QuestionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Tag = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WrittenQuestionTags", x => new { x.QuestionId, x.Tag });
                    table.ForeignKey(
                        name: "FK_WrittenQuestionTags_WrittenQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "WrittenQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WrittenAnswerRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    AnswerId = table.Column<Guid>(type: "char(36)", nullable: false),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false),
                    WordCount = table.Column<int>(type: "int", nullable: false),
                    TimeSpentSeconds = table.Column<int>(type: "int", nullable: false),
                    SelfAssessmentJson = table.Column<string>(type: "longtext", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WrittenAnswerRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WrittenAnswerRevisions_WrittenAnswers_AnswerId",
                        column: x => x.AnswerId,
                        principalTable: "WrittenAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WrittenModelAnswerReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    ModelAnswerId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    Publisher = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false),
                    Url = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    Citation = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    VerificationStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WrittenModelAnswerReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WrittenModelAnswerReferences_WrittenModelAnswers_ModelAnswer~",
                        column: x => x.ModelAnswerId,
                        principalTable: "WrittenModelAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_WrittenAnswerRevisions_AnswerId_RevisionNumber",
                table: "WrittenAnswerRevisions",
                columns: new[] { "AnswerId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WrittenAnswers_QuestionId",
                table: "WrittenAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_WrittenAnswers_UserId_QuestionId_Status",
                table: "WrittenAnswers",
                columns: new[] { "UserId", "QuestionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WrittenAnswers_UserId_UpdatedAt",
                table: "WrittenAnswers",
                columns: new[] { "UserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WrittenBookmarks_QuestionId",
                table: "WrittenBookmarks",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_WrittenGoals_UserId_StartDate_EndDate",
                table: "WrittenGoals",
                columns: new[] { "UserId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WrittenModelAnswerReferences_ModelAnswerId",
                table: "WrittenModelAnswerReferences",
                column: "ModelAnswerId");

            migrationBuilder.CreateIndex(
                name: "IX_WrittenModelAnswers_QuestionId",
                table: "WrittenModelAnswers",
                column: "QuestionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WrittenModelAnswers_UpdatedById",
                table: "WrittenModelAnswers",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WrittenQuestionResources_QuestionId_Url",
                table: "WrittenQuestionResources",
                columns: new[] { "QuestionId", "Url" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WrittenQuestions_BcsExamYear",
                table: "WrittenQuestions",
                column: "BcsExamYear");

            migrationBuilder.CreateIndex(
                name: "IX_WrittenQuestions_CreatedById",
                table: "WrittenQuestions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WrittenQuestions_Status_Type",
                table: "WrittenQuestions",
                columns: new[] { "Status", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_WrittenQuestions_SyllabusItemId",
                table: "WrittenQuestions",
                column: "SyllabusItemId");

            migrationBuilder.CreateIndex(
                name: "IX_WrittenQuestionSourceReferences_QuestionId",
                table: "WrittenQuestionSourceReferences",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_WrittenQuestionTags_Tag",
                table: "WrittenQuestionTags",
                column: "Tag");

            migrationBuilder.CreateIndex(
                name: "IX_WrittenStudyNotes_SyllabusItemId",
                table: "WrittenStudyNotes",
                column: "SyllabusItemId");

            migrationBuilder.CreateIndex(
                name: "IX_WrittenStudyNotes_UserId_SyllabusItemId_UpdatedAt",
                table: "WrittenStudyNotes",
                columns: new[] { "UserId", "SyllabusItemId", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WrittenAnswerRevisions");

            migrationBuilder.DropTable(
                name: "WrittenBookmarks");

            migrationBuilder.DropTable(
                name: "WrittenGoals");

            migrationBuilder.DropTable(
                name: "WrittenModelAnswerReferences");

            migrationBuilder.DropTable(
                name: "WrittenQuestionResources");

            migrationBuilder.DropTable(
                name: "WrittenQuestionSourceReferences");

            migrationBuilder.DropTable(
                name: "WrittenQuestionTags");

            migrationBuilder.DropTable(
                name: "WrittenStudyNotes");

            migrationBuilder.DropTable(
                name: "WrittenAnswers");

            migrationBuilder.DropTable(
                name: "WrittenModelAnswers");

            migrationBuilder.DropTable(
                name: "WrittenQuestions");
        }
    }
}
