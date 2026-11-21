using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace BCSStudyHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Description = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
                    Name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    DisplayName = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UserName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: true),
                    SecurityStamp = table.Column<string>(type: "longtext", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "longtext", nullable: true),
                    PhoneNumber = table.Column<string>(type: "longtext", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ActorId = table.Column<Guid>(type: "char(36)", nullable: true),
                    Action = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true),
                    EntityId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CorrelationId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

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
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

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

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Code = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "varchar(240)", maxLength: 240, nullable: false),
                    Description = table.Column<string>(type: "longtext", maxLength: 2000, nullable: true),
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
                    Notes = table.Column<string>(type: "longtext", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyllabusSourceReferences", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

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
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    RoleId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ClaimType = table.Column<string>(type: "longtext", nullable: true),
                    ClaimValue = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ClaimType = table.Column<string>(type: "longtext", nullable: true),
                    ClaimValue = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "varchar(255)", nullable: false),
                    ProviderKey = table.Column<string>(type: "varchar(255)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "longtext", nullable: true),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    RoleId = table.Column<Guid>(type: "char(36)", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    LoginProvider = table.Column<string>(type: "varchar(255)", nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false),
                    Value = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LibraryCollections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    OwnerId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryCollections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryCollections_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LibraryResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    OwnerId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Kind = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    Visibility = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    Title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "longtext", maxLength: 4000, nullable: true),
                    Authors = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    SourceName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    SourceType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    Website = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    Url = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true),
                    PublicationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RightsBasis = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    RightsNote = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    NoteContent = table.Column<string>(type: "longtext", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryResources_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OfficialSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    LocalizedName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    Category = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    Url = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    Description = table.Column<string>(type: "longtext", maxLength: 2000, nullable: true),
                    ParentOrganization = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    IsInternational = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UsageNotes = table.Column<string>(type: "longtext", maxLength: 2000, nullable: true),
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
                    FeedbackNotes = table.Column<string>(type: "longtext", maxLength: 4000, nullable: true),
                    ImprovementAreas = table.Column<string>(type: "longtext", maxLength: 4000, nullable: true),
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
                name: "SyllabusVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    ExamStageId = table.Column<Guid>(type: "char(36)", nullable: false),
                    VersionLabel = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    TrackCode = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    Summary = table.Column<string>(type: "longtext", maxLength: 2000, nullable: false),
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
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "char(36)", nullable: false),
                    PermissionId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "VivaQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    CategoryId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Topic = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: true),
                    Prompt = table.Column<string>(type: "longtext", maxLength: 10000, nullable: false),
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
                name: "LibraryBookChapters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    ResourceId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ChapterNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    Summary = table.Column<string>(type: "longtext", maxLength: 4000, nullable: true),
                    StartPage = table.Column<int>(type: "int", nullable: true),
                    EndPage = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryBookChapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryBookChapters_LibraryResources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "LibraryResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LibraryBookDetails",
                columns: table => new
                {
                    ResourceId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Publisher = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    Edition = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true),
                    PublicationYear = table.Column<int>(type: "int", nullable: true),
                    Isbn = table.Column<string>(type: "varchar(13)", maxLength: 13, nullable: true),
                    PageCount = table.Column<int>(type: "int", nullable: true),
                    Language = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryBookDetails", x => x.ResourceId);
                    table.ForeignKey(
                        name: "FK_LibraryBookDetails_LibraryResources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "LibraryResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LibraryCollectionItems",
                columns: table => new
                {
                    CollectionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ResourceId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryCollectionItems", x => new { x.CollectionId, x.ResourceId });
                    table.ForeignKey(
                        name: "FK_LibraryCollectionItems_LibraryCollections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "LibraryCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibraryCollectionItems_LibraryResources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "LibraryResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LibraryFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    ResourceId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Role = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    Format = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    OriginalFileName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    ContentType = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false),
                    StorageProvider = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    StorageKey = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    UploadedById = table.Column<Guid>(type: "char(36)", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryFiles_AspNetUsers_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LibraryFiles_LibraryResources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "LibraryResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LibraryResourceSubjects",
                columns: table => new
                {
                    ResourceId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SubjectId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryResourceSubjects", x => new { x.ResourceId, x.SubjectId });
                    table.ForeignKey(
                        name: "FK_LibraryResourceSubjects_LibraryResources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "LibraryResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibraryResourceSubjects_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LibraryResourceTags",
                columns: table => new
                {
                    ResourceId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Tag = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryResourceTags", x => new { x.ResourceId, x.Tag });
                    table.ForeignKey(
                        name: "FK_LibraryResourceTags_LibraryResources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "LibraryResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CurrentAffairsEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    Summary = table.Column<string>(type: "longtext", maxLength: 2000, nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: true),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SourcePublishedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Category = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    SourceName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    SourceUrl = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false),
                    OfficialSourceId = table.Column<Guid>(type: "char(36)", nullable: true),
                    SourceStatus = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    Notes = table.Column<string>(type: "longtext", maxLength: 4000, nullable: true),
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
                    PromptSnapshot = table.Column<string>(type: "longtext", maxLength: 10000, nullable: false),
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
                name: "LibraryBookmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ResourceId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ChapterId = table.Column<Guid>(type: "char(36)", nullable: true),
                    Page = table.Column<int>(type: "int", nullable: true),
                    Label = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryBookmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryBookmarks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibraryBookmarks_LibraryBookChapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "LibraryBookChapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LibraryBookmarks_LibraryResources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "LibraryResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LibraryChapterResources",
                columns: table => new
                {
                    ChapterId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ResourceId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryChapterResources", x => new { x.ChapterId, x.ResourceId });
                    table.ForeignKey(
                        name: "FK_LibraryChapterResources_LibraryBookChapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "LibraryBookChapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibraryChapterResources_LibraryResources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "LibraryResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LibraryPersonalNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ResourceId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ChapterId = table.Column<Guid>(type: "char(36)", nullable: true),
                    Page = table.Column<int>(type: "int", nullable: true),
                    Content = table.Column<string>(type: "longtext", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryPersonalNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryPersonalNotes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibraryPersonalNotes_LibraryBookChapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "LibraryBookChapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LibraryPersonalNotes_LibraryResources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "LibraryResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LibraryReadingProgress",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ResourceId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    CurrentChapterId = table.Column<Guid>(type: "char(36)", nullable: true),
                    CurrentPage = table.Column<int>(type: "int", nullable: true),
                    TotalPages = table.Column<int>(type: "int", nullable: true),
                    PercentComplete = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryReadingProgress", x => new { x.UserId, x.ResourceId });
                    table.ForeignKey(
                        name: "FK_LibraryReadingProgress_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibraryReadingProgress_LibraryBookChapters_CurrentChapterId",
                        column: x => x.CurrentChapterId,
                        principalTable: "LibraryBookChapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LibraryReadingProgress_LibraryResources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "LibraryResources",
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
                name: "CurrentAffairsRevisions",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    EntryId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ReviewCount = table.Column<int>(type: "int", nullable: false),
                    Step = table.Column<int>(type: "int", nullable: false),
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
                    Description = table.Column<string>(type: "longtext", maxLength: 4000, nullable: true),
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
                name: "LibraryChapterTopics",
                columns: table => new
                {
                    ChapterId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SyllabusItemId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryChapterTopics", x => new { x.ChapterId, x.SyllabusItemId });
                    table.ForeignKey(
                        name: "FK_LibraryChapterTopics_LibraryBookChapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "LibraryBookChapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibraryChapterTopics_SyllabusItems_SyllabusItemId",
                        column: x => x.SyllabusItemId,
                        principalTable: "SyllabusItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LibraryResourceTopics",
                columns: table => new
                {
                    ResourceId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SyllabusItemId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryResourceTopics", x => new { x.ResourceId, x.SyllabusItemId });
                    table.ForeignKey(
                        name: "FK_LibraryResourceTopics_LibraryResources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "LibraryResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibraryResourceTopics_SyllabusItems_SyllabusItemId",
                        column: x => x.SyllabusItemId,
                        principalTable: "SyllabusItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "McqExamSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    SyllabusVersionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SubjectId = table.Column<Guid>(type: "char(36)", nullable: true),
                    SyllabusItemId = table.Column<Guid>(type: "char(36)", nullable: true),
                    Name = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false),
                    Instructions = table.Column<string>(type: "longtext", maxLength: 3000, nullable: false),
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
                    Prompt = table.Column<string>(type: "longtext", maxLength: 5000, nullable: false),
                    Explanation = table.Column<string>(type: "longtext", maxLength: 8000, nullable: true),
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
                name: "MistakeEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SourceType = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    QuestionId = table.Column<Guid>(type: "char(36)", nullable: true),
                    QuestionText = table.Column<string>(type: "longtext", maxLength: 8000, nullable: false),
                    YourAnswer = table.Column<string>(type: "longtext", maxLength: 4000, nullable: true),
                    CorrectAnswerReference = table.Column<string>(type: "longtext", maxLength: 4000, nullable: false),
                    Explanation = table.Column<string>(type: "longtext", maxLength: 8000, nullable: true),
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
                    Mastery = table.Column<int>(type: "int", nullable: true),
                    MasteryUpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "WrittenQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    SyllabusItemId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Prompt = table.Column<string>(type: "longtext", maxLength: 12000, nullable: false),
                    Type = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    BcsExamYear = table.Column<int>(type: "int", nullable: true),
                    ExamNumber = table.Column<int>(type: "int", nullable: true),
                    ExamReference = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: true),
                    Marks = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    AnswerGuidance = table.Column<string>(type: "longtext", maxLength: 8000, nullable: true),
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
                    Text = table.Column<string>(type: "longtext", maxLength: 2000, nullable: false),
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
                    Description = table.Column<string>(type: "longtext", maxLength: 4000, nullable: true),
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
                    PromptSnapshot = table.Column<string>(type: "longtext", maxLength: 5000, nullable: false),
                    ExplanationSnapshot = table.Column<string>(type: "longtext", maxLength: 8000, nullable: true),
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
                    Notes = table.Column<string>(type: "longtext", maxLength: 2000, nullable: true),
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
                    Notes = table.Column<string>(type: "longtext", maxLength: 2000, nullable: true),
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
                    TextSnapshot = table.Column<string>(type: "longtext", maxLength: 2000, nullable: false),
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
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorId_OccurredAt",
                table: "AuditLogs",
                columns: new[] { "ActorId", "OccurredAt" });

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
                name: "IX_ExamStages_Code",
                table: "ExamStages",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibraryBookChapters_ResourceId_ChapterNumber",
                table: "LibraryBookChapters",
                columns: new[] { "ResourceId", "ChapterNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibraryBookDetails_Isbn",
                table: "LibraryBookDetails",
                column: "Isbn");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryBookmarks_ChapterId",
                table: "LibraryBookmarks",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryBookmarks_ResourceId",
                table: "LibraryBookmarks",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryBookmarks_UserId_CreatedAt",
                table: "LibraryBookmarks",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LibraryBookmarks_UserId_ResourceId",
                table: "LibraryBookmarks",
                columns: new[] { "UserId", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_LibraryChapterResources_ResourceId",
                table: "LibraryChapterResources",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryChapterTopics_SyllabusItemId",
                table: "LibraryChapterTopics",
                column: "SyllabusItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryCollectionItems_ResourceId",
                table: "LibraryCollectionItems",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryCollections_OwnerId_Name",
                table: "LibraryCollections",
                columns: new[] { "OwnerId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibraryFiles_ResourceId_Role",
                table: "LibraryFiles",
                columns: new[] { "ResourceId", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibraryFiles_StorageKey",
                table: "LibraryFiles",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibraryFiles_UploadedById",
                table: "LibraryFiles",
                column: "UploadedById");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryPersonalNotes_ChapterId",
                table: "LibraryPersonalNotes",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryPersonalNotes_ResourceId",
                table: "LibraryPersonalNotes",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryPersonalNotes_UserId_ResourceId_UpdatedAt",
                table: "LibraryPersonalNotes",
                columns: new[] { "UserId", "ResourceId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LibraryReadingProgress_CurrentChapterId",
                table: "LibraryReadingProgress",
                column: "CurrentChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryReadingProgress_ResourceId",
                table: "LibraryReadingProgress",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryReadingProgress_UserId_Status_UpdatedAt",
                table: "LibraryReadingProgress",
                columns: new[] { "UserId", "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LibraryResources_OwnerId_Visibility",
                table: "LibraryResources",
                columns: new[] { "OwnerId", "Visibility" });

            migrationBuilder.CreateIndex(
                name: "IX_LibraryResources_Title",
                table: "LibraryResources",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryResources_UpdatedAt",
                table: "LibraryResources",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryResources_Visibility_Status_Kind",
                table: "LibraryResources",
                columns: new[] { "Visibility", "Status", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_LibraryResourceSubjects_SubjectId",
                table: "LibraryResourceSubjects",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryResourceTags_Tag",
                table: "LibraryResourceTags",
                column: "Tag");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryResourceTopics_SyllabusItemId",
                table: "LibraryResourceTopics",
                column: "SyllabusItemId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Papers_SyllabusSubjectId_Code",
                table: "Papers",
                columns: new[] { "SyllabusSubjectId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true);

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
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

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
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogs");

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
                name: "LibraryBookDetails");

            migrationBuilder.DropTable(
                name: "LibraryBookmarks");

            migrationBuilder.DropTable(
                name: "LibraryChapterResources");

            migrationBuilder.DropTable(
                name: "LibraryChapterTopics");

            migrationBuilder.DropTable(
                name: "LibraryCollectionItems");

            migrationBuilder.DropTable(
                name: "LibraryFiles");

            migrationBuilder.DropTable(
                name: "LibraryPersonalNotes");

            migrationBuilder.DropTable(
                name: "LibraryReadingProgress");

            migrationBuilder.DropTable(
                name: "LibraryResourceSubjects");

            migrationBuilder.DropTable(
                name: "LibraryResourceTags");

            migrationBuilder.DropTable(
                name: "LibraryResourceTopics");

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
                name: "MistakeEntries");

            migrationBuilder.DropTable(
                name: "OfficialSourceTags");

            migrationBuilder.DropTable(
                name: "PlannerSettings");

            migrationBuilder.DropTable(
                name: "RevisionLogs");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SiteSettings");

            migrationBuilder.DropTable(
                name: "StudyGoals");

            migrationBuilder.DropTable(
                name: "StudySessions");

            migrationBuilder.DropTable(
                name: "SyllabusItemSources");

            migrationBuilder.DropTable(
                name: "SyllabusVersionSources");

            migrationBuilder.DropTable(
                name: "TopicProgress");

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
                name: "CurrentAffairsEntries");

            migrationBuilder.DropTable(
                name: "LibraryCollections");

            migrationBuilder.DropTable(
                name: "LibraryBookChapters");

            migrationBuilder.DropTable(
                name: "McqAttemptQuestions");

            migrationBuilder.DropTable(
                name: "McqQuestions");

            migrationBuilder.DropTable(
                name: "RevisionItems");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "StudyTasks");

            migrationBuilder.DropTable(
                name: "SyllabusSourceReferences");

            migrationBuilder.DropTable(
                name: "VivaAnswerNotes");

            migrationBuilder.DropTable(
                name: "VivaMockSessions");

            migrationBuilder.DropTable(
                name: "WrittenAnswers");

            migrationBuilder.DropTable(
                name: "WrittenModelAnswers");

            migrationBuilder.DropTable(
                name: "OfficialSources");

            migrationBuilder.DropTable(
                name: "LibraryResources");

            migrationBuilder.DropTable(
                name: "McqAttempts");

            migrationBuilder.DropTable(
                name: "StudySchedules");

            migrationBuilder.DropTable(
                name: "VivaQuestions");

            migrationBuilder.DropTable(
                name: "WrittenQuestions");

            migrationBuilder.DropTable(
                name: "McqExamSets");

            migrationBuilder.DropTable(
                name: "VivaCategories");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

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
