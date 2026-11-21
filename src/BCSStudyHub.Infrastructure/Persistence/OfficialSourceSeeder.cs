using BCSStudyHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BCSStudyHub.Infrastructure.Persistence;

/// <summary>
/// Seeds the official resource directory. Each landing page was fetched on <see cref="CheckedOn"/> and listed as
/// verified only when the page itself identified the organisation. Sources whose check failed are seeded as
/// unpublished drafts so an editor can verify them in a browser. Rows are inserted only when their URL is missing,
/// so editor changes are never overwritten.
/// </summary>
public sealed class OfficialSourceSeeder(ApplicationDbContext dbContext, ILogger<OfficialSourceSeeder> logger)
{
    public static readonly DateOnly CheckedOn = new(2026, 11, 21);

    private const string UsageNote =
        "Link to the official page instead of copying its content. Check the site's terms before reusing text, data, or documents.";

    private sealed record Seed(
        string Name,
        string? LocalizedName,
        OfficialSourceCategory Category,
        string Url,
        string Description,
        string? Parent,
        bool International,
        string? Failure,
        string? ExtraNote = null);

    private static readonly Seed[] Sources =
    [
        new("Bangladesh Public Service Commission", "বাংলাদেশ সরকারী কর্ম কমিশন", OfficialSourceCategory.ConstitutionalBody, "https://bpsc.gov.bd/",
            "Constitutional body that conducts the BCS examinations; publishes circulars, syllabi, notices, and results.", null, false, null),
        new("Bangladesh Election Commission", null, OfficialSourceCategory.ConstitutionalBody, "https://www.ecs.gov.bd/",
            "Constitutional body responsible for national and local elections.", null, false, "HTTP 403 Forbidden"),
        new("Bangladesh National Portal", "বাংলাদেশ জাতীয় তথ্য বাতায়ন", OfficialSourceCategory.GovernmentPortal, "https://bangladesh.gov.bd/",
            "Government of Bangladesh national portal linking ministries, divisions, and public services.", null, false, null),
        new("Cabinet Division", "মন্ত্রিপরিষদ বিভাগ", OfficialSourceCategory.Ministry, "https://cabinet.gov.bd/",
            "Coordinates cabinet business and government-wide administration.", null, false, null),
        new("Ministry of Public Administration", "জনপ্রশাসন মন্ত্রণালয়", OfficialSourceCategory.Ministry, "https://mopa.gov.bd/",
            "Responsible for the civil service and public administration.", null, false, null),
        new("Finance Division", "অর্থ বিভাগ, অর্থ মন্ত্রণালয়", OfficialSourceCategory.Ministry, "https://mof.gov.bd/",
            "Finance Division of the Ministry of Finance; publishes national budget documents.", "Ministry of Finance", false, null),
        new("Economic Relations Division", "অর্থনৈতিক সম্পর্ক বিভাগ", OfficialSourceCategory.Ministry, "https://erd.gov.bd/",
            "Handles external development assistance and economic relations.", "Ministry of Finance", false, null),
        new("Ministry of Foreign Affairs", "পররাষ্ট্র মন্ত্রণালয়", OfficialSourceCategory.Ministry, "https://mofa.gov.bd/",
            "Foreign policy, diplomacy, and international relations of Bangladesh.", null, false, null),
        new("Ministry of Environment, Forest and Climate Change", "পরিবেশ, বন ও জলবায়ু পরিবর্তন মন্ত্রণালয়", OfficialSourceCategory.Ministry, "https://moef.gov.bd/",
            "Environment, forestry, and climate-change policy.", null, false, null),
        new("Ministry of Education", "শিক্ষা মন্ত্রণালয়", OfficialSourceCategory.Ministry, "https://moedu.gov.bd/",
            "Education policy and administration.", null, false, null),
        new("Planning Commission", null, OfficialSourceCategory.Department, "https://plancomm.gov.bd/",
            "National planning body; publishes development plans.", "Ministry of Planning", false, "TLS certificate could not be verified"),
        new("Department of Printing and Publications", null, OfficialSourceCategory.Department, "https://www.dpp.gov.bd/",
            "Government printing and publications department.", null, false, null,
            "The page states that Bangladesh Government Press (publisher of the Bangladesh Gazette) is a unit of DPP and that the current website is dpp.portal.gov.bd, which has not been verified separately."),
        new("Bangladesh Bank", null, OfficialSourceCategory.CentralBank, "https://www.bb.org.bd/",
            "The central bank of Bangladesh; publishes monetary policy statements, reports, and economic data.", null, false, null),
        new("Bangladesh Bureau of Statistics", null, OfficialSourceCategory.Statistics, "https://bbs.gov.bd/",
            "National statistics office; publishes census, survey, and economic statistics.", null, false, "TLS certificate could not be verified"),
        new("Bangladesh Parliament (Jatiya Sangsad)", null, OfficialSourceCategory.Parliament, "https://www.parliament.gov.bd/",
            "Official website of the national parliament.", null, false, "TLS certificate could not be verified"),
        new("Laws of Bangladesh", null, OfficialSourceCategory.LegalAndLegislative, "http://bdlaws.minlaw.gov.bd/",
            "Official online database of Bangladesh laws, including the Constitution.", "Legislative and Parliamentary Affairs Division", false, null,
            "At verification the site was served over HTTP only (no HTTPS upgrade)."),
        new("Legislative and Parliamentary Affairs Division", "লেজিসলেটিভ ও সংসদ বিষয়ক বিভাগ", OfficialSourceCategory.LegalAndLegislative, "https://legislativediv.gov.bd/",
            "Drafts legislation and maintains the official legal texts.", "Ministry of Law, Justice and Parliamentary Affairs", false, null),
        new("Supreme Court of Bangladesh", null, OfficialSourceCategory.Judiciary, "https://www.supremecourt.gov.bd/",
            "Official website of the apex court; judgments, notices, and cause lists.", null, false, null),
        new("Bangladesh Sangbad Sangstha (BSS)", null, OfficialSourceCategory.OfficialNews, "https://www.bssnews.net/",
            "The national news agency of Bangladesh.", null, false, null),
        new("Press Information Department", null, OfficialSourceCategory.OfficialNews, "https://pressinform.gov.bd/",
            "Issues official government press releases.", "Ministry of Information and Broadcasting", false, "TLS certificate could not be verified"),
        new("United Nations", null, OfficialSourceCategory.InternationalOrganization, "https://www.un.org/",
            "Official website of the United Nations.", null, true, "HTTP 403 Forbidden"),
        new("United Nations in Bangladesh", null, OfficialSourceCategory.InternationalOrganization, "https://bangladesh.un.org/",
            "The UN country team and Resident Coordinator's office in Bangladesh.", "United Nations", true, null),
        new("UNDP Bangladesh", null, OfficialSourceCategory.InternationalOrganization, "https://www.undp.org/bangladesh",
            "United Nations Development Programme country office.", "United Nations", true, "HTTP 403 Forbidden"),
        new("World Bank in Bangladesh", null, OfficialSourceCategory.InternationalOrganization, "https://www.worldbank.org/en/country/bangladesh",
            "World Bank country page: projects, data, and reports on Bangladesh.", "World Bank Group", true, null),
        new("Asian Development Bank – Bangladesh", null, OfficialSourceCategory.InternationalOrganization, "https://www.adb.org/countries/bangladesh/main",
            "ADB country page and Bangladesh Resident Mission.", "Asian Development Bank", true, null),
        new("International Monetary Fund – Bangladesh", null, OfficialSourceCategory.InternationalOrganization, "https://www.imf.org/en/Countries/BGD",
            "IMF country page: programme documents, reports, and data on Bangladesh.", "International Monetary Fund", true, null),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = (await dbContext.OfficialSources.Select(source => source.Url).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var order = new Dictionary<OfficialSourceCategory, int>();
        var added = 0;
        foreach (var seed in Sources)
        {
            order[seed.Category] = order.GetValueOrDefault(seed.Category) + 10;
            if (existing.Contains(seed.Url))
            {
                continue;
            }

            var verified = seed.Failure is null;
            dbContext.OfficialSources.Add(new OfficialSource
            {
                Id = Guid.NewGuid(),
                Name = seed.Name,
                LocalizedName = seed.LocalizedName,
                Category = seed.Category,
                Url = seed.Url,
                Description = seed.Description,
                ParentOrganization = seed.Parent,
                IsInternational = seed.International,
                UsageNotes = UsageNote,
                SortOrder = order[seed.Category],
                VerificationStatus = verified ? OfficialSourceVerificationStatus.Verified : OfficialSourceVerificationStatus.Unverified,
                LastVerifiedOn = verified ? CheckedOn : null,
                VerificationNote = verified
                    ? $"Landing page fetched on {CheckedOn:yyyy-MM-dd}; the page identifies the organisation.{(seed.ExtraNote is null ? string.Empty : " " + seed.ExtraNote)}"
                    : $"Automated check on {CheckedOn:yyyy-MM-dd} failed ({seed.Failure}). Verify the URL in a browser before publishing.",
                Status = verified ? OfficialSourceStatus.Published : OfficialSourceStatus.Draft,
            });
            added++;
        }

        if (added > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} official resource directory entries.", added);
        }
    }
}
