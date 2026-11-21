using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BCSStudyHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVivaPreparation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VivaCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Slug = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VivaCategories", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VivaMockSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Title = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    CommunicationRating = table.Column<int>(type: "int", nullable: true),
                    SubjectKnowledgeRating = table.Column<int>(type: "int", nullable: true),
                    ClarityRating = table.Column<int>(type: "int", nullable: true),
                    ConfidenceRating = table.Column<int>(type: "int", nullable: true),
                    FeedbackNotes = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true),
                    ImprovementAreas = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VivaMockSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VivaMockSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VivaProfilePreparations",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SelfIntroduction = table.Column<string>(type: "longtext", nullable: false),
                    AcademicBackground = table.Column<string>(type: "longtext", nullable: false),
                    UniversityAndSubject = table.Column<string>(type: "longtext", nullable: false),
                    HomeDistrict = table.Column<string>(type: "longtext", nullable: false),
                    ProfessionalExperience = table.Column<string>(type: "longtext", nullable: false),
                    CadrePreferences = table.Column<string>(type: "longtext", nullable: false),
                    InterestsAndHobbies = table.Column<string>(type: "longtext", nullable: false),
                    StrengthsAndDevelopmentAreas = table.Column<string>(type: "longtext", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VivaProfilePreparations", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_VivaProfilePreparations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VivaQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    CategoryId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Topic = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: true),
                    Prompt = table.Column<string>(type: "varchar(10000)", maxLength: 10000, nullable: false),
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
                    table.PrimaryKey("PK_VivaQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VivaQuestions_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VivaQuestions_VivaCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "VivaCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VivaAnswerNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    QuestionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VivaAnswerNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VivaAnswerNotes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VivaAnswerNotes_VivaQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "VivaQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VivaBookmarks",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    QuestionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VivaBookmarks", x => new { x.UserId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_VivaBookmarks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VivaBookmarks_VivaQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "VivaQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VivaMockSessionQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    SessionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    QuestionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    PromptSnapshot = table.Column<string>(type: "varchar(10000)", maxLength: 10000, nullable: false),
                    CategorySnapshot = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    TopicSnapshot = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: true),
                    AnswerNotes = table.Column<string>(type: "longtext", nullable: false),
                    NeedsRevision = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VivaMockSessionQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VivaMockSessionQuestions_VivaMockSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "VivaMockSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VivaMockSessionQuestions_VivaQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "VivaQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VivaQuestionHistory",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    QuestionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    LastViewedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VivaQuestionHistory", x => new { x.UserId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_VivaQuestionHistory_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VivaQuestionHistory_VivaQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "VivaQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VivaQuestionReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    QuestionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Type = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    Title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    Publisher = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false),
                    Url = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    Citation = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    VerificationStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VivaQuestionReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VivaQuestionReferences_VivaQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "VivaQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VivaAnswerNoteRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    AnswerNoteId = table.Column<Guid>(type: "char(36)", nullable: false),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VivaAnswerNoteRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VivaAnswerNoteRevisions_VivaAnswerNotes_AnswerNoteId",
                        column: x => x.AnswerNoteId,
                        principalTable: "VivaAnswerNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_VivaAnswerNoteRevisions_AnswerNoteId_RevisionNumber",
                table: "VivaAnswerNoteRevisions",
                columns: new[] { "AnswerNoteId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VivaAnswerNotes_QuestionId",
                table: "VivaAnswerNotes",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_VivaAnswerNotes_UserId_QuestionId",
                table: "VivaAnswerNotes",
                columns: new[] { "UserId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VivaBookmarks_QuestionId",
                table: "VivaBookmarks",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_VivaCategories_IsActive_DisplayOrder",
                table: "VivaCategories",
                columns: new[] { "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_VivaCategories_Slug",
                table: "VivaCategories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VivaMockSessionQuestions_NeedsRevision_TopicSnapshot",
                table: "VivaMockSessionQuestions",
                columns: new[] { "NeedsRevision", "TopicSnapshot" });

            migrationBuilder.CreateIndex(
                name: "IX_VivaMockSessionQuestions_QuestionId",
                table: "VivaMockSessionQuestions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_VivaMockSessionQuestions_SessionId_QuestionId",
                table: "VivaMockSessionQuestions",
                columns: new[] { "SessionId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VivaMockSessionQuestions_SessionId_Sequence",
                table: "VivaMockSessionQuestions",
                columns: new[] { "SessionId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VivaMockSessions_UserId_StartedAt",
                table: "VivaMockSessions",
                columns: new[] { "UserId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VivaQuestionHistory_QuestionId",
                table: "VivaQuestionHistory",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_VivaQuestionHistory_UserId_LastViewedAt",
                table: "VivaQuestionHistory",
                columns: new[] { "UserId", "LastViewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VivaQuestionReferences_QuestionId_Type",
                table: "VivaQuestionReferences",
                columns: new[] { "QuestionId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_VivaQuestions_BcsExamYear",
                table: "VivaQuestions",
                column: "BcsExamYear");

            migrationBuilder.CreateIndex(
                name: "IX_VivaQuestions_CategoryId",
                table: "VivaQuestions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_VivaQuestions_CreatedById",
                table: "VivaQuestions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_VivaQuestions_Status_CategoryId",
                table: "VivaQuestions",
                columns: new[] { "Status", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_VivaQuestions_Topic_Status",
                table: "VivaQuestions",
                columns: new[] { "Topic", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VivaAnswerNoteRevisions");

            migrationBuilder.DropTable(
                name: "VivaBookmarks");

            migrationBuilder.DropTable(
                name: "VivaMockSessionQuestions");

            migrationBuilder.DropTable(
                name: "VivaProfilePreparations");

            migrationBuilder.DropTable(
                name: "VivaQuestionHistory");

            migrationBuilder.DropTable(
                name: "VivaQuestionReferences");

            migrationBuilder.DropTable(
                name: "VivaAnswerNotes");

            migrationBuilder.DropTable(
                name: "VivaMockSessions");

            migrationBuilder.DropTable(
                name: "VivaQuestions");

            migrationBuilder.DropTable(
                name: "VivaCategories");
        }
    }
}
