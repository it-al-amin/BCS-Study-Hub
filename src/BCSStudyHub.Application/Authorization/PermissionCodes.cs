namespace BCSStudyHub.Application.Authorization;

public static class PermissionCodes
{
    public const string DashboardRead = "dashboard.read";
    public const string SyllabusRead = "syllabus.read";
    public const string AdminAccess = "admin.access";
    public const string UsersManage = "users.manage";
    public const string ContentManage = "content.manage";
    public const string SyllabusManage = "syllabus.manage";
    public const string McqManage = "mcq.manage";
    public const string WrittenManage = "written.manage";
    public const string VivaManage = "viva.manage";
    public const string LibraryManage = "library.manage";

    public static IReadOnlyDictionary<string, string> Descriptions { get; } =
        new Dictionary<string, string>
        {
            [DashboardRead] = "View the study dashboard",
            [SyllabusRead] = "View published syllabus content",
            [AdminAccess] = "Access administrative endpoints",
            [UsersManage] = "Manage user accounts and roles",
            [ContentManage] = "Manage study content",
            [SyllabusManage] = "Manage syllabus stages and content",
            [McqManage] = "Manage Preliminary MCQ questions and practice sets",
            [WrittenManage] = "Manage BCS Written questions and model answers",
            [VivaManage] = "Manage Viva categories and questions",
            [LibraryManage] = "Manage shared digital library resources, files, and chapters",
        };
}
