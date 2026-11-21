using BCSStudyHub.Application.Auth;
using BCSStudyHub.Infrastructure.Auth;
using BCSStudyHub.Infrastructure.Configuration;
using BCSStudyHub.Infrastructure.Identity;
using BCSStudyHub.Infrastructure.Persistence;
using BCSStudyHub.Infrastructure.Syllabus;
using BCSStudyHub.Infrastructure.Mcq;
using BCSStudyHub.Infrastructure.Written;
using BCSStudyHub.Application.Mcq;
using BCSStudyHub.Application.Syllabus;
using BCSStudyHub.Application.Written;
using BCSStudyHub.Infrastructure.Viva;
using BCSStudyHub.Application.Viva;
using BCSStudyHub.Application.Library;
using BCSStudyHub.Infrastructure.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using BCSStudyHub.Application.CurrentAffairs;
using BCSStudyHub.Infrastructure.CurrentAffairs;
using BCSStudyHub.Application.Planner;
using BCSStudyHub.Infrastructure.Planner;
using BCSStudyHub.Application.Admin;
using BCSStudyHub.Infrastructure.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BCSStudyHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MySql");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'MySql' is required. Configure it using user-secrets or the ConnectionStrings__MySql environment variable.");
        }

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("JWT configuration is required.");
        jwtSettings.Validate();

        services.AddSingleton(jwtSettings);
        services.AddDbContext<ApplicationDbContext>(options => options.UseMySQL(connectionString));
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<ApplicationRole>()
            .AddSignInManager()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddHttpContextAccessor();
        services.AddScoped<JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISyllabusService, SyllabusService>();
        services.AddScoped<IMcqService, McqService>();
        services.AddScoped<IWrittenService, WrittenService>();
        services.AddScoped<IVivaService, VivaService>();

        var libraryStorage = configuration.GetSection(LibraryStorageOptions.SectionName).Get<LibraryStorageOptions>()
            ?? new LibraryStorageOptions();
        libraryStorage.Validate();
        services.AddSingleton(libraryStorage);
        services.AddSingleton<IFileStorage>(provider =>
        {
            var contentRoot = provider.GetRequiredService<IHostEnvironment>().ContentRootPath;
            return new LocalFileStorage(Path.Combine(contentRoot, libraryStorage.RootPath));
        });
        services.AddScoped<ILibraryService, LibraryService>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ICurrentAffairsService, CurrentAffairsService>();
        services.AddScoped<IOfficialSourceService, OfficialSourceService>();
        services.AddScoped<PlannerSettingsStore>();
        services.AddScoped<StudyMetrics>();
        services.AddScoped<PlannerService>();
        services.AddScoped<IPlannerService>(provider => provider.GetRequiredService<PlannerService>());
        services.AddScoped<IRevisionService, RevisionService>();
        services.AddScoped<IProgressService, ProgressService>();
        services.AddScoped<ISiteSettingsService, SiteSettingsService>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<IAdminOverviewService, AdminOverviewService>();
        services.AddScoped<ITagAdminService, TagAdminService>();

        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<SyllabusDataSeeder>();
        services.AddScoped<OfficialSourceSeeder>();

        return services;
    }
}
