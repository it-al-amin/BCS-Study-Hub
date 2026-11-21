namespace BCSStudyHub.Application.Authorization;

public static class PermissionCodes
{
    public const string DashboardRead = "dashboard.read";
    public const string SyllabusRead = "syllabus.read";
    public const string AdminAccess = "admin.access";
    public const string UsersManage = "users.manage";
    public const string ContentManage = "content.manage";
    public const string SyllabusManage = "syllabus.manage";

    public static IReadOnlyDictionary<string, string> Descriptions { get; } =
        new Dictionary<string, string>
        {
            [DashboardRead] = "View the study dashboard",
            [SyllabusRead] = "View published syllabus content",
            [AdminAccess] = "Access administrative endpoints",
            [UsersManage] = "Manage user accounts and roles",
            [ContentManage] = "Manage study content",
            [SyllabusManage] = "Manage syllabus stages and content",
        };
}
