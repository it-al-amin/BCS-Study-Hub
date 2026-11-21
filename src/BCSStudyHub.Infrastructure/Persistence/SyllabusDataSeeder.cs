using BCSStudyHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Infrastructure.Persistence;

public sealed class SyllabusDataSeeder(ApplicationDbContext dbContext)
{
    private const string BpscExamPage =
        "https://bpsc.gov.bd/pages/static-pages/691997b3933eb65569dde2bf";
    private const string PreliminaryPdf =
        "https://objectstorage.ap-dcc-gazipur-1.oraclecloud15.com/n/axvjbnqprylg/b/V2Ministry/o/office-bpsc/2024/12/f3385505e6c844f59d74c226a4b4a95e.pdf";
    private const string WrittenPdf =
        "https://objectstorage.ap-dcc-gazipur-1.oraclecloud15.com/n/axvjbnqprylg/b/V2Ministry/o/office-bpsc/2024/12/36342dd1ff6e4c418894b0ae354c9c65.pdf";
    private const string ExamSpecificPreliminaryPage =
        "https://bpsc.gov.bd/pages/psc-exams/৫০তম-বি-সি-এস-পরীক্ষা-২০২৫-এর-প্রিলিমিনারি-টেস্টের-mcq-type-সিলেবাস-299ca4-69568a5d35ce18e1c05ad0af";
    private const string ExamSpecificPreliminaryPdf =
        "https://objectstorage.ap-dcc-gazipur-1.oraclecloud15.com/n/axvjbnqprylg/b/V2Ministry/o/office-bpsc/2024/12/be22c1ddda60414fad98b0a82080ef9a.pdf";

    private static readonly DateOnly SourceVerifiedOn = new(2026, 11, 21);

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var stages = new[]
        {
            new ExamStage
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                Code = "Preliminary",
                Name = "Preliminary",
                LocalizedName = "প্রিলিমিনারি",
                SortOrder = 1,
            },
            new ExamStage
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                Code = "Written",
                Name = "Written",
                LocalizedName = "লিখিত",
                SortOrder = 2,
            },
            new ExamStage
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                Code = "Viva",
                Name = "Viva",
                LocalizedName = "মৌখিক",
                SortOrder = 3,
            },
        };

        foreach (var stage in stages)
        {
            if (!await dbContext.ExamStages.AnyAsync(candidate => candidate.Code == stage.Code, cancellationToken))
            {
                dbContext.ExamStages.Add(stage);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var sources = await SeedSourcesAsync(cancellationToken);
        await SeedVersionAsync(
            "Preliminary",
            "BPSC published subject outline",
            "Common",
            "BPSC publishes a 200-mark preliminary test and the ten subject groups and marks listed here. Detailed topic text from the linked PDF has not been transcribed or verified in this release.",
            [
                ("PRELIM_BANGLA", "বাংলা ভাষা ও সাহিত্য", 35),
                ("PRELIM_ENGLISH", "ইংরেজি ভাষা ও সাহিত্য", 35),
                ("PRELIM_BANGLADESH", "বাংলাদেশ বিষয়াবলি", 30),
                ("PRELIM_INTERNATIONAL", "আন্তর্জাতিক বিষয়াবলি", 20),
                ("PRELIM_GEOGRAPHY", "ভূগোল (বাংলাদেশ ও বিশ্ব), পরিবেশ ও দুর্যোগ ব্যবস্থাপনা", 10),
                ("PRELIM_SCIENCE", "সাধারণ বিজ্ঞান", 15),
                ("PRELIM_ICT", "কম্পিউটার ও তথ্য প্রযুক্তি", 15),
                ("PRELIM_MATH", "গাণিতিক যুক্তি", 15),
                ("PRELIM_MENTAL", "মানসিক দক্ষতা", 15),
                ("PRELIM_ETHICS", "নৈতিকতা, মূল্যবোধ ও সুশাসন", 10),
            ],
            [sources[BpscExamPage], sources[PreliminaryPdf]],
            cancellationToken);

        await SeedVersionAsync(
            "Written",
            "BPSC published subject outline",
            "General",
            "BPSC publishes this 900-mark compulsory subject and marks outline for general-cadre candidates. Detailed paper and topic content linked by BPSC is not transcribed or verified in this release.",
            [
                ("WRITTEN_BANGLA", "বাংলা", 200),
                ("WRITTEN_ENGLISH", "ইংরেজি", 200),
                ("WRITTEN_BANGLADESH", "বাংলাদেশ বিষয়াবলি", 200),
                ("WRITTEN_INTERNATIONAL", "আন্তর্জাতিক বিষয়াবলি", 100),
                ("WRITTEN_MATH_MENTAL", "গাণিতিক যুক্তি ও মানসিক দক্ষতা", 100),
                ("WRITTEN_SCIENCE_TECH", "সাধারণ বিজ্ঞান ও প্রযুক্তি", 100),
            ],
            [sources[BpscExamPage], sources[WrittenPdf]],
            cancellationToken);

        await SeedVersionAsync(
            "Written",
            "BPSC published subject outline",
            "TechnicalProfessional",
            "BPSC publishes this 900-mark subject and marks outline for technical/professional-cadre candidates. The post-related subject is intentionally left generic; no subject-specific paper or topic has been inferred.",
            [
                ("WRITTEN_BANGLA", "বাংলা", 100),
                ("WRITTEN_ENGLISH", "ইংরেজি", 200),
                ("WRITTEN_BANGLADESH", "বাংলাদেশ বিষয়াবলি", 200),
                ("WRITTEN_INTERNATIONAL", "আন্তর্জাতিক বিষয়াবলি", 100),
                ("WRITTEN_MATH_MENTAL", "গাণিতিক যুক্তি ও মানসিক দক্ষতা", 100),
                ("WRITTEN_POST_RELATED", "পদ-সংশ্লিষ্ট বিষয়", 200),
            ],
            [sources[BpscExamPage], sources[WrittenPdf]],
            cancellationToken);

        await SeedVersionAsync(
            "Viva",
            "BPSC published oral-exam structure",
            "Common",
            "The BPSC examination page specifies a 200-mark oral examination and a 50% pass mark. It does not provide a topic-by-topic Viva syllabus on that page, so no interview topics are presented as official.",
            [],
            [sources[BpscExamPage]],
            cancellationToken);
    }

    private async Task<Dictionary<string, SyllabusSourceReference>> SeedSourcesAsync(
        CancellationToken cancellationToken)
    {
        var sourceSpecs = new[]
        {
            new SyllabusSourceReference
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                Title = "BPSC BCS examination structure and subject marks",
                Publisher = "Bangladesh Public Service Commission",
                Url = BpscExamPage,
                SourceType = "OfficialPage",
                LastVerifiedOn = SourceVerifiedOn,
                VerificationStatus = SourceVerificationStatus.Verified,
                Notes = "BPSC page, last updated 13 May 2025. The stage and subject/marks outlines were checked against this page.",
            },
            new SyllabusSourceReference
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                Title = "Preliminary test detailed syllabus (BPSC-linked PDF)",
                Publisher = "Bangladesh Public Service Commission",
                Url = PreliminaryPdf,
                SourceType = "OfficialPdf",
                LastVerifiedOn = SourceVerifiedOn,
                VerificationStatus = SourceVerificationStatus.Verified,
                Notes = "The BPSC examination page links this PDF. Its detailed topic text is not imported into this release.",
            },
            new SyllabusSourceReference
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000003"),
                Title = "Written examination detailed syllabus (BPSC-linked PDF)",
                Publisher = "Bangladesh Public Service Commission",
                Url = WrittenPdf,
                SourceType = "OfficialPdf",
                LastVerifiedOn = SourceVerifiedOn,
                VerificationStatus = SourceVerificationStatus.Verified,
                Notes = "The BPSC examination page links this PDF. Detailed papers and topics are not imported into this release.",
            },
            new SyllabusSourceReference
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000004"),
                Title = "50th BCS preliminary syllabus (exam-specific BPSC page)",
                Publisher = "Bangladesh Public Service Commission",
                Url = ExamSpecificPreliminaryPage,
                SourceType = "OfficialPage",
                PublishedOn = new DateOnly(2025, 12, 9),
                LastVerifiedOn = SourceVerifiedOn,
                VerificationStatus = SourceVerificationStatus.Verified,
                Notes = "This is an exam-specific 2025 syllabus, not assumed to be a universal syllabus for future BCS examinations.",
            },
            new SyllabusSourceReference
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000005"),
                Title = "50th BCS preliminary syllabus attachment (exam-specific BPSC PDF)",
                Publisher = "Bangladesh Public Service Commission",
                Url = ExamSpecificPreliminaryPdf,
                SourceType = "OfficialPdf",
                LastVerifiedOn = SourceVerifiedOn,
                VerificationStatus = SourceVerificationStatus.Verified,
                Notes = "BPSC's exam-specific 50th BCS attachment; not applied to the common Preliminary version.",
            },
        };

        foreach (var source in sourceSpecs)
        {
            var exists = await dbContext.SyllabusSourceReferences.AnyAsync(
                candidate => candidate.Url == source.Url,
                cancellationToken);
            if (!exists)
            {
                dbContext.SyllabusSourceReferences.Add(source);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await dbContext.SyllabusSourceReferences
            .Where(source => sourceSpecs.Select(spec => spec.Url).Contains(source.Url))
            .ToDictionaryAsync(source => source.Url, cancellationToken);
    }

    private async Task SeedVersionAsync(
        string stageCode,
        string label,
        string trackCode,
        string summary,
        IReadOnlyList<(string Code, string Name, int? Marks)> subjects,
        IReadOnlyList<SyllabusSourceReference> sources,
        CancellationToken cancellationToken)
    {
        var stage = await dbContext.ExamStages.SingleAsync(
            candidate => candidate.Code == stageCode,
            cancellationToken);
        var exists = await dbContext.SyllabusVersions.AnyAsync(
            version => version.ExamStageId == stage.Id &&
                       version.TrackCode == trackCode &&
                       version.VersionLabel == label,
            cancellationToken);
        if (exists)
        {
            return;
        }

        var version = new SyllabusVersion
        {
            Id = Guid.NewGuid(),
            ExamStageId = stage.Id,
            VersionLabel = label,
            TrackCode = trackCode,
            Summary = summary,
            Coverage = SyllabusCoverage.OutlineOnly,
            Status = SyllabusVersionStatus.Published,
            PublishedAt = DateTimeOffset.UtcNow,
        };
        dbContext.SyllabusVersions.Add(version);

        for (var index = 0; index < subjects.Count; index++)
        {
            var (code, name, marks) = subjects[index];
            var subject = await dbContext.Subjects.SingleOrDefaultAsync(
                candidate => candidate.Code == code,
                cancellationToken);
            if (subject is null)
            {
                subject = new Subject
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    Name = name,
                };
                dbContext.Subjects.Add(subject);
            }

            version.Subjects.Add(new SyllabusSubject
            {
                Id = Guid.NewGuid(),
                SubjectId = subject.Id,
                SortOrder = index + 1,
                Marks = marks,
                IsRequired = true,
            });
        }

        foreach (var source in sources)
        {
            var trackedSource = await dbContext.SyllabusSourceReferences.SingleAsync(
                candidate => candidate.Url == source.Url,
                cancellationToken);
            version.SourceReferences.Add(new SyllabusVersionSource
            {
                SourceReferenceId = trackedSource.Id,
                Notes = "Official source linked for the verified outline; see source notes for coverage limits.",
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
