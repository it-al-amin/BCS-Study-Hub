using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BCSStudyHub.Infrastructure.Persistence;

public static class MySqlOptionsExtensions
{
    /// <summary>
    /// Uses MySQL/MariaDB but skips the migration advisory lock (GET_LOCK), which the Oracle provider
    /// fails on with "Unable to cast DBNull to Int64" on MariaDB and some managed servers.
    /// </summary>
    public static DbContextOptionsBuilder<TContext> UseMySqlWithoutMigrationLock<TContext>(
        this DbContextOptionsBuilder<TContext> builder,
        string connectionString) where TContext : DbContext
    {
        ((DbContextOptionsBuilder)builder).UseMySqlWithoutMigrationLock(connectionString);
        return builder;
    }

    // Without an explicit charset the connection follows the server default (often latin1), which turns
    // Bengali text into '?' on write.
    private static string WithUtf8(string connectionString) =>
        new MySql.Data.MySqlClient.MySqlConnectionStringBuilder(connectionString) { CharacterSet = "utf8mb4" }.ConnectionString;

    public static DbContextOptionsBuilder UseMySqlWithoutMigrationLock(
        this DbContextOptionsBuilder builder,
        string connectionString) =>
        builder.UseMySQL(WithUtf8(connectionString)).ReplaceService<IHistoryRepository, NoLockHistoryRepository>();
}

internal sealed class NoLockHistoryRepository : IHistoryRepository
{
    private readonly IHistoryRepository _inner;

    public NoLockHistoryRepository(HistoryRepositoryDependencies dependencies)
    {
        var type = Type.GetType("MySql.EntityFrameworkCore.Migrations.Internal.MySQLHistoryRepository, MySql.EntityFrameworkCore")
            ?? throw new InvalidOperationException("MySQL history repository type was not found.");
        _inner = (IHistoryRepository)Activator.CreateInstance(type, dependencies)!;
    }

    public IMigrationsDatabaseLock AcquireDatabaseLock() => new NoOpLock(_inner);

    public Task<IMigrationsDatabaseLock> AcquireDatabaseLockAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IMigrationsDatabaseLock>(new NoOpLock(_inner));

    public LockReleaseBehavior LockReleaseBehavior => _inner.LockReleaseBehavior;
    public void Create() => _inner.Create();
    public Task CreateAsync(CancellationToken cancellationToken = default) => _inner.CreateAsync(cancellationToken);
    public bool Exists() => _inner.Exists();
    public Task<bool> ExistsAsync(CancellationToken cancellationToken = default) => _inner.ExistsAsync(cancellationToken);
    public IReadOnlyList<HistoryRow> GetAppliedMigrations() => _inner.GetAppliedMigrations();
    public Task<IReadOnlyList<HistoryRow>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default) =>
        _inner.GetAppliedMigrationsAsync(cancellationToken);
    public string GetCreateScript() => _inner.GetCreateScript();
    public string GetCreateIfNotExistsScript() => _inner.GetCreateIfNotExistsScript();
    public string GetInsertScript(HistoryRow row) => _inner.GetInsertScript(row);
    public string GetDeleteScript(string migrationId) => _inner.GetDeleteScript(migrationId);
    public string GetBeginIfNotExistsScript(string migrationId) => _inner.GetBeginIfNotExistsScript(migrationId);
    public string GetBeginIfExistsScript(string migrationId) => _inner.GetBeginIfExistsScript(migrationId);
    public string GetEndIfScript() => _inner.GetEndIfScript();

    private sealed class NoOpLock(IHistoryRepository owner) : IMigrationsDatabaseLock
    {
        public IHistoryRepository HistoryRepository { get; } = owner;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
