using System.ComponentModel.DataAnnotations;
using BCSStudyHub.Application.Admin;
using BCSStudyHub.Application.ContentSources;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.ContentSources;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Api.Tests;

public sealed class ContentSourceTests : IDisposable
{
    private readonly ApplicationDbContext dbContext = new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly ContentSourceService service;
    private readonly Guid actor = Guid.NewGuid();

    public ContentSourceTests() =>
        service = new ContentSourceService(dbContext, new HttpContextAccessor(), TimeProvider.System);

    private static ContentSourceInput Valid(string url = "https://bpsc.gov.bd") =>
        new() { Name = "BPSC", BaseUrl = url, Category = "Official" };

    private static bool IsValid(ContentSourceInput input) =>
        Validator.TryValidateObject(input, new ValidationContext(input), [], true);

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://localhost")]
    [InlineData("https://127.0.0.1")]
    [InlineData("https://10.0.0.5")]
    [InlineData("https://192.168.1.1")]
    [InlineData("https://169.254.169.254/latest/meta-data")]
    [InlineData("https://metadata.google.internal")]
    [InlineData("https://printer.local")]
    [InlineData("https://user:pass@example.com")]
    [InlineData("https://example.com:8443")]
    [InlineData("https://intranet")]
    [InlineData("ftp://example.com")]
    public void UnsafeUrlsAreRejected(string url) => Assert.False(IsValid(Valid(url)));

    [Fact]
    public void PublicHttpsUrlsAreAccepted() => Assert.True(IsValid(Valid()));

    [Fact]
    public void FeedAndFullTextRulesAreEnforced()
    {
        Assert.False(IsValid(Valid() with { ExtractionMethod = "Feed" }));
        Assert.True(IsValid(Valid() with { ExtractionMethod = "Feed", FeedUrl = "https://bpsc.gov.bd/rss.xml" }));
        Assert.False(IsValid(Valid() with { RetentionPolicy = "FullTextPermitted" }));
        Assert.True(IsValid(Valid() with { RetentionPolicy = "FullTextPermitted", RightsNote = "CC BY 4.0" }));
        Assert.False(IsValid(Valid() with { SourceType = "7" }));
    }

    [Fact]
    public async Task NewSourcesStartDisabledAndDuplicatesAreRejected()
    {
        var created = await service.CreateAsync(Valid(), actor, default);
        Assert.False(created.IsEnabled);
        Assert.Equal("NeverScanned", created.Health);
        await Assert.ThrowsAsync<AdminException>(() => service.CreateAsync(Valid(), actor, default));
    }

    [Fact]
    public async Task ArchivedSourcesCannotBeEnabledOrScannedAndOnlyArchivedCanBeDeleted()
    {
        var source = await service.CreateAsync(Valid(), actor, default);
        await service.SetEnabledAsync(source.Id, true, actor, default);
        await Assert.ThrowsAsync<AdminException>(() => service.DeleteAsync(source.Id, actor, default));

        var archived = await service.ArchiveAsync(source.Id, actor, default);
        Assert.False(archived.IsEnabled);
        await Assert.ThrowsAsync<AdminException>(() => service.SetEnabledAsync(source.Id, true, actor, default));
        await Assert.ThrowsAsync<AdminException>(() => service.RequestScanAsync(source.Id, actor, default));
        await service.DeleteAsync(source.Id, actor, default);
        Assert.Empty(dbContext.ContentSources);
    }

    [Fact]
    public async Task OnlyOneScanCanBePendingAndFailedScansCanBeRetried()
    {
        var source = await service.CreateAsync(Valid(), actor, default);
        var scan = await service.RequestScanAsync(source.Id, actor, default);
        Assert.Equal("Queued", scan.Status);
        await Assert.ThrowsAsync<AdminException>(() => service.RequestScanAsync(source.Id, actor, default));
        await Assert.ThrowsAsync<AdminException>(() => service.RetryScanAsync(scan.Id, actor, default));

        var row = await dbContext.ContentSourceScans.SingleAsync();
        row.Status = ContentScanStatus.Failed;
        await dbContext.SaveChangesAsync();
        var retry = await service.RetryScanAsync(scan.Id, actor, default);
        Assert.Equal("Retry", retry.Trigger);
        Assert.Equal(2, (await service.ListScansAsync(source.Id, null, 1, 10, default)).TotalCount);
    }

    [Fact]
    public async Task SummaryAndFiltersReflectStatus()
    {
        var one = await service.CreateAsync(Valid(), actor, default);
        await service.CreateAsync(Valid("https://www.bangladesh.gov.bd"), actor, default);
        await service.SetEnabledAsync(one.Id, true, actor, default);
        var summary = await service.GetSummaryAsync(default);
        Assert.Equal(2, summary.Total);
        Assert.Equal(1, summary.Enabled);
        Assert.Equal(1, summary.Disabled);
        Assert.Single((await service.ListAsync(new ContentSourceQuery(null, null, null, true), default)).Items);
    }

    public void Dispose() => dbContext.Dispose();
}
