using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BCSStudyHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDigitalLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    Description = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true),
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
                name: "LibraryBookChapters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    ResourceId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ChapterNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    Summary = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "LibraryCollections");

            migrationBuilder.DropTable(
                name: "LibraryBookChapters");

            migrationBuilder.DropTable(
                name: "LibraryResources");
        }
    }
}
