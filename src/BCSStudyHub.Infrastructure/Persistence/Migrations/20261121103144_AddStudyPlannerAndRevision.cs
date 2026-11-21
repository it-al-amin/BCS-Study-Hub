using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BCSStudyHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudyPlannerAndRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Mastery",
                table: "TopicProgress",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MasteryUpdatedAt",
                table: "TopicProgress",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Step",
                table: "CurrentAffairsRevisions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MistakeEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SourceType = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    QuestionId = table.Column<Guid>(type: "char(36)", nullable: true),
                    QuestionText = table.Column<string>(type: "varchar(8000)", maxLength: 8000, nullable: false),
                    YourAnswer = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true),
                    CorrectAnswerReference = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false),
                    Explanation = table.Column<string>(type: "varchar(8000)", maxLength: 8000, nullable: true),
                    Category = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    SyllabusItemId = table.Column<Guid>(type: "char(36)", nullable: true),
                    ReviewOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MistakeEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MistakeEntries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MistakeEntries_SyllabusItems_SyllabusItemId",
                        column: x => x.SyllabusItemId,
                        principalTable: "SyllabusItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlannerSettings",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    RevisionIntervalsJson = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: false),
                    AgainStep = table.Column<int>(type: "int", nullable: false),
                    HardStepDelta = table.Column<int>(type: "int", nullable: false),
                    GoodStepDelta = table.Column<int>(type: "int", nullable: false),
                    EasyStepDelta = table.Column<int>(type: "int", nullable: false),
                    RetireAfterStep = table.Column<int>(type: "int", nullable: true),
                    DailyRevisionLimit = table.Column<int>(type: "int", nullable: false),
                    StreakMinimumMinutes = table.Column<int>(type: "int", nullable: false),
                    WeakTopicAccuracyPercent = table.Column<int>(type: "int", nullable: false),
                    WeakTopicMinimumAnswers = table.Column<int>(type: "int", nullable: false),
                    AutoQueueMistakes = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerSettings", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_PlannerSettings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RevisionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SourceType = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    SourceId = table.Column<Guid>(type: "char(36)", nullable: true),
                    Title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    SyllabusItemId = table.Column<Guid>(type: "char(36)", nullable: true),
                    Reason = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    Step = table.Column<int>(type: "int", nullable: false),
                    IntervalDays = table.Column<int>(type: "int", nullable: false),
                    DueOn = table.Column<DateOnly>(type: "date", nullable: false),
                    ReviewCount = table.Column<int>(type: "int", nullable: false),
                    LapseCount = table.Column<int>(type: "int", nullable: false),
                    LastOutcome = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true),
                    LastReviewedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevisionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevisionItems_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RevisionItems_SyllabusItems_SyllabusItemId",
                        column: x => x.SyllabusItemId,
                        principalTable: "SyllabusItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StudyGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Period = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Metric = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    TargetValue = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<Guid>(type: "char(36)", nullable: true),
                    Stage = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true),
                    Notes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyGoals_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudyGoals_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StudySchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    DaysOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time(6)", nullable: true),
                    PlannedMinutes = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    SubjectId = table.Column<Guid>(type: "char(36)", nullable: true),
                    SyllabusItemId = table.Column<Guid>(type: "char(36)", nullable: true),
                    Stage = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudySchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudySchedules_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudySchedules_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudySchedules_SyllabusItems_SyllabusItemId",
                        column: x => x.SyllabusItemId,
                        principalTable: "SyllabusItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RevisionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    RevisionItemId = table.Column<Guid>(type: "char(36)", nullable: true),
                    SourceType = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    SourceId = table.Column<Guid>(type: "char(36)", nullable: true),
                    Title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    SyllabusItemId = table.Column<Guid>(type: "char(36)", nullable: true),
                    Outcome = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    StepBefore = table.Column<int>(type: "int", nullable: false),
                    StepAfter = table.Column<int>(type: "int", nullable: false),
                    IntervalDays = table.Column<int>(type: "int", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevisionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevisionLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RevisionLogs_RevisionItems_RevisionItemId",
                        column: x => x.RevisionItemId,
                        principalTable: "RevisionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StudyTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true),
                    Priority = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time(6)", nullable: true),
                    PlannedMinutes = table.Column<int>(type: "int", nullable: false),
                    ActualMinutes = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<Guid>(type: "char(36)", nullable: true),
                    SyllabusItemId = table.Column<Guid>(type: "char(36)", nullable: true),
                    Stage = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true),
                    ScheduleId = table.Column<Guid>(type: "char(36)", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyTasks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudyTasks_StudySchedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "StudySchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudyTasks_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudyTasks_SyllabusItems_SyllabusItemId",
                        column: x => x.SyllabusItemId,
                        principalTable: "SyllabusItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StudySessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Activity = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    TaskId = table.Column<Guid>(type: "char(36)", nullable: true),
                    SubjectId = table.Column<Guid>(type: "char(36)", nullable: true),
                    SyllabusItemId = table.Column<Guid>(type: "char(36)", nullable: true),
                    Stage = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true),
                    Notes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudySessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudySessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudySessions_StudyTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "StudyTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudySessions_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudySessions_SyllabusItems_SyllabusItemId",
                        column: x => x.SyllabusItemId,
                        principalTable: "SyllabusItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_MistakeEntries_SyllabusItemId",
                table: "MistakeEntries",
                column: "SyllabusItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MistakeEntries_UserId_SourceType_QuestionId",
                table: "MistakeEntries",
                columns: new[] { "UserId", "SourceType", "QuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_MistakeEntries_UserId_Status_UpdatedAt",
                table: "MistakeEntries",
                columns: new[] { "UserId", "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RevisionItems_SyllabusItemId",
                table: "RevisionItems",
                column: "SyllabusItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisionItems_UserId_SourceType_SourceId",
                table: "RevisionItems",
                columns: new[] { "UserId", "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_RevisionItems_UserId_Status_DueOn",
                table: "RevisionItems",
                columns: new[] { "UserId", "Status", "DueOn" });

            migrationBuilder.CreateIndex(
                name: "IX_RevisionLogs_RevisionItemId",
                table: "RevisionLogs",
                column: "RevisionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisionLogs_UserId_ReviewedAt",
                table: "RevisionLogs",
                columns: new[] { "UserId", "ReviewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RevisionLogs_UserId_SourceType_SourceId",
                table: "RevisionLogs",
                columns: new[] { "UserId", "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyGoals_SubjectId",
                table: "StudyGoals",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyGoals_UserId_Period_PeriodStart",
                table: "StudyGoals",
                columns: new[] { "UserId", "Period", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_StudySchedules_SubjectId",
                table: "StudySchedules",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_StudySchedules_SyllabusItemId",
                table: "StudySchedules",
                column: "SyllabusItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StudySchedules_UserId_IsActive",
                table: "StudySchedules",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_SubjectId",
                table: "StudySessions",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_SyllabusItemId",
                table: "StudySessions",
                column: "SyllabusItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_TaskId",
                table: "StudySessions",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_UserId_EndedAt",
                table: "StudySessions",
                columns: new[] { "UserId", "EndedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_UserId_StartedAt",
                table: "StudySessions",
                columns: new[] { "UserId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyTasks_ScheduleId_ScheduledDate",
                table: "StudyTasks",
                columns: new[] { "ScheduleId", "ScheduledDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyTasks_SubjectId",
                table: "StudyTasks",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyTasks_SyllabusItemId",
                table: "StudyTasks",
                column: "SyllabusItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyTasks_UserId_ScheduledDate",
                table: "StudyTasks",
                columns: new[] { "UserId", "ScheduledDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyTasks_UserId_Status",
                table: "StudyTasks",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MistakeEntries");

            migrationBuilder.DropTable(
                name: "PlannerSettings");

            migrationBuilder.DropTable(
                name: "RevisionLogs");

            migrationBuilder.DropTable(
                name: "StudyGoals");

            migrationBuilder.DropTable(
                name: "StudySessions");

            migrationBuilder.DropTable(
                name: "RevisionItems");

            migrationBuilder.DropTable(
                name: "StudyTasks");

            migrationBuilder.DropTable(
                name: "StudySchedules");

            migrationBuilder.DropColumn(
                name: "Mastery",
                table: "TopicProgress");

            migrationBuilder.DropColumn(
                name: "MasteryUpdatedAt",
                table: "TopicProgress");

            migrationBuilder.DropColumn(
                name: "Step",
                table: "CurrentAffairsRevisions");
        }
    }
}
