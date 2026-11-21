using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BCSStudyHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMcqPracticeEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "McqExamSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    SyllabusVersionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SubjectId = table.Column<Guid>(type: "char(36)", nullable: true),
                    SyllabusItemId = table.Column<Guid>(type: "char(36)", nullable: true),
                    Name = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false),
                    Instructions = table.Column<string>(type: "varchar(3000)", maxLength: 3000, nullable: false),
                    Mode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    QuestionCount = table.Column<int>(type: "int", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    BcsExamYear = table.Column<int>(type: "int", nullable: true),
                    Difficulty = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    MarksCorrect = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    MarksIncorrect = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    MarksUnanswered = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    ShowFeedbackImmediately = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedById = table.Column<Guid>(type: "char(36)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McqExamSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_McqExamSets_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_McqExamSets_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_McqExamSets_SyllabusItems_SyllabusItemId",
                        column: x => x.SyllabusItemId,
                        principalTable: "SyllabusItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_McqExamSets_SyllabusVersions_SyllabusVersionId",
                        column: x => x.SyllabusVersionId,
                        principalTable: "SyllabusVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "McqQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    SyllabusItemId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Prompt = table.Column<string>(type: "varchar(5000)", maxLength: 5000, nullable: false),
                    Explanation = table.Column<string>(type: "varchar(8000)", maxLength: 8000, nullable: true),
                    ImageUrl = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true),
                    Type = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    Difficulty = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    BcsExamYear = table.Column<int>(type: "int", nullable: true),
                    ExamNumber = table.Column<int>(type: "int", nullable: true),
                    ExamReference = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: true),
                    CreatedById = table.Column<Guid>(type: "char(36)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McqQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_McqQuestions_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_McqQuestions_SyllabusItems_SyllabusItemId",
                        column: x => x.SyllabusItemId,
                        principalTable: "SyllabusItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "McqAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ExamSetId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ExamSetNameSnapshot = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false),
                    Mode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    MarksCorrect = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    MarksIncorrect = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    MarksUnanswered = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    ShowFeedbackImmediately = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsTimedOut = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    MaximumScore = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    CorrectCount = table.Column<int>(type: "int", nullable: false),
                    IncorrectCount = table.Column<int>(type: "int", nullable: false),
                    UnansweredCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McqAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_McqAttempts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_McqAttempts_McqExamSets_ExamSetId",
                        column: x => x.ExamSetId,
                        principalTable: "McqExamSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "McqBookmarks",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    QuestionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McqBookmarks", x => new { x.UserId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_McqBookmarks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_McqBookmarks_McqQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "McqQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "McqQuestionOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    QuestionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Text = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsCorrect = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McqQuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_McqQuestionOptions_McqQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "McqQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "McqQuestionSourceReferences",
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
                    table.PrimaryKey("PK_McqQuestionSourceReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_McqQuestionSourceReferences_McqQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "McqQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "McqQuestionTags",
                columns: table => new
                {
                    QuestionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Tag = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McqQuestionTags", x => new { x.QuestionId, x.Tag });
                    table.ForeignKey(
                        name: "FK_McqQuestionTags_McqQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "McqQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "McqAttemptQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    AttemptId = table.Column<Guid>(type: "char(36)", nullable: false),
                    QuestionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SyllabusItemIdSnapshot = table.Column<Guid>(type: "char(36)", nullable: false),
                    TopicTitleSnapshot = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    ParentTopicTitleSnapshot = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
                    Position = table.Column<int>(type: "int", nullable: false),
                    PromptSnapshot = table.Column<string>(type: "varchar(5000)", maxLength: 5000, nullable: false),
                    ExplanationSnapshot = table.Column<string>(type: "varchar(8000)", maxLength: 8000, nullable: true),
                    ImageUrlSnapshot = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true),
                    SourcesSnapshotJson = table.Column<string>(type: "longtext", nullable: false),
                    Type = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    Difficulty = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McqAttemptQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_McqAttemptQuestions_McqAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "McqAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "McqMistakes",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    QuestionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    LastAttemptId = table.Column<Guid>(type: "char(36)", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McqMistakes", x => new { x.UserId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_McqMistakes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_McqMistakes_McqAttempts_LastAttemptId",
                        column: x => x.LastAttemptId,
                        principalTable: "McqAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_McqMistakes_McqQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "McqQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "McqAttemptAnswers",
                columns: table => new
                {
                    AttemptQuestionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SelectedOptionIdsJson = table.Column<string>(type: "longtext", nullable: false),
                    IsMarkedForReview = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsCorrect = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    MarksAwarded = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McqAttemptAnswers", x => x.AttemptQuestionId);
                    table.ForeignKey(
                        name: "FK_McqAttemptAnswers_McqAttemptQuestions_AttemptQuestionId",
                        column: x => x.AttemptQuestionId,
                        principalTable: "McqAttemptQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "McqAttemptOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    AttemptQuestionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SourceOptionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    TextSnapshot = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsCorrect = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McqAttemptOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_McqAttemptOptions_McqAttemptQuestions_AttemptQuestionId",
                        column: x => x.AttemptQuestionId,
                        principalTable: "McqAttemptQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_McqAttemptOptions_AttemptQuestionId_SortOrder",
                table: "McqAttemptOptions",
                columns: new[] { "AttemptQuestionId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_McqAttemptQuestions_AttemptId_Position",
                table: "McqAttemptQuestions",
                columns: new[] { "AttemptId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_McqAttemptQuestions_AttemptId_QuestionId",
                table: "McqAttemptQuestions",
                columns: new[] { "AttemptId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_McqAttempts_ExamSetId",
                table: "McqAttempts",
                column: "ExamSetId");

            migrationBuilder.CreateIndex(
                name: "IX_McqAttempts_UserId_StartedAt",
                table: "McqAttempts",
                columns: new[] { "UserId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_McqAttempts_UserId_Status",
                table: "McqAttempts",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_McqBookmarks_QuestionId",
                table: "McqBookmarks",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_McqExamSets_CreatedById",
                table: "McqExamSets",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_McqExamSets_Status_Mode",
                table: "McqExamSets",
                columns: new[] { "Status", "Mode" });

            migrationBuilder.CreateIndex(
                name: "IX_McqExamSets_SubjectId",
                table: "McqExamSets",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_McqExamSets_SyllabusItemId",
                table: "McqExamSets",
                column: "SyllabusItemId");

            migrationBuilder.CreateIndex(
                name: "IX_McqExamSets_SyllabusVersionId",
                table: "McqExamSets",
                column: "SyllabusVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_McqMistakes_LastAttemptId",
                table: "McqMistakes",
                column: "LastAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_McqMistakes_QuestionId",
                table: "McqMistakes",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_McqMistakes_UserId_Status_UpdatedAt",
                table: "McqMistakes",
                columns: new[] { "UserId", "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_McqQuestionOptions_QuestionId_SortOrder",
                table: "McqQuestionOptions",
                columns: new[] { "QuestionId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_McqQuestions_BcsExamYear",
                table: "McqQuestions",
                column: "BcsExamYear");

            migrationBuilder.CreateIndex(
                name: "IX_McqQuestions_CreatedById",
                table: "McqQuestions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_McqQuestions_ExamNumber",
                table: "McqQuestions",
                column: "ExamNumber");

            migrationBuilder.CreateIndex(
                name: "IX_McqQuestions_Status_Difficulty",
                table: "McqQuestions",
                columns: new[] { "Status", "Difficulty" });

            migrationBuilder.CreateIndex(
                name: "IX_McqQuestions_SyllabusItemId",
                table: "McqQuestions",
                column: "SyllabusItemId");

            migrationBuilder.CreateIndex(
                name: "IX_McqQuestionSourceReferences_QuestionId",
                table: "McqQuestionSourceReferences",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_McqQuestionTags_Tag",
                table: "McqQuestionTags",
                column: "Tag");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "McqAttemptAnswers");

            migrationBuilder.DropTable(
                name: "McqAttemptOptions");

            migrationBuilder.DropTable(
                name: "McqBookmarks");

            migrationBuilder.DropTable(
                name: "McqMistakes");

            migrationBuilder.DropTable(
                name: "McqQuestionOptions");

            migrationBuilder.DropTable(
                name: "McqQuestionSourceReferences");

            migrationBuilder.DropTable(
                name: "McqQuestionTags");

            migrationBuilder.DropTable(
                name: "McqAttemptQuestions");

            migrationBuilder.DropTable(
                name: "McqQuestions");

            migrationBuilder.DropTable(
                name: "McqAttempts");

            migrationBuilder.DropTable(
                name: "McqExamSets");
        }
    }
}
