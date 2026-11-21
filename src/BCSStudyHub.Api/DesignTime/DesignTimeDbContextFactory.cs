using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BCSStudyHub.Api.DesignTime;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<ApiAssemblyMarker>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("MySql");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'MySql' is required. Configure it using user-secrets or the ConnectionStrings__MySql environment variable.");
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySQL(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }
}

public sealed class ApiAssemblyMarker
{
}
