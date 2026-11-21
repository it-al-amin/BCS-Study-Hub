# BPSC syllabus source register

## Verification scope

The initial database seed contains only subject names, stage structure, and mark allocations that were visible on BPSC's official BCS examination page. The stored subject names preserve the page's Bengali wording. Each seeded syllabus version is marked `OutlineOnly`.

The linked detailed PDFs are official references, but their detailed topic text has not been transcribed into the database in this release. No topic or subtopic is inferred from study guides, coaching materials, or exam-specific documents. The admin workflow marks new syllabus items `Unverified` by default and requires an item-level verified source before an item can be marked verified. A version claiming `FullTopics` coverage cannot be published unless every item is source-verified.

## Official references checked on 2026-11-21

1. **BPSC: BCS examination structure and subject marks**  
   https://bpsc.gov.bd/pages/static-pages/691997b3933eb65569dde2bf  
   BPSC's page was last updated 13 May 2025. It describes the three examination stages, preliminary subject/mark distribution, general and technical/professional written subject/mark distributions, and the 200-mark oral examination and pass mark.

2. **BPSC-linked detailed Preliminary syllabus PDF**  
   https://objectstorage.ap-dcc-gazipur-1.oraclecloud15.com/n/axvjbnqprylg/b/V2Ministry/o/office-bpsc/2024/12/f3385505e6c844f59d74c226a4b4a95e.pdf  
   Linked from the BPSC BCS examination page. This release records the link but does not publish its detailed topic text as verified content.

3. **BPSC-linked detailed Written syllabus PDF**  
   https://objectstorage.ap-dcc-gazipur-1.oraclecloud15.com/n/axvjbnqprylg/b/V2Ministry/o/office-bpsc/2024/12/36342dd1ff6e4c418894b0ae354c9c65.pdf  
   Linked from the BPSC BCS examination page. This release records the link but does not publish its detailed paper/topic text as verified content.

4. **BPSC: 50th BCS 2025 Preliminary syllabus notice**  
   https://bpsc.gov.bd/pages/psc-exams/৫০তম-বি-সি-এস-পরীক্ষা-২০২৫-এর-প্রিলিমিনারি-টেস্টের-mcq-type-সিলেবাস-299ca4-69568a5d35ce18e1c05ad0af  
   BPSC published this exam-specific notice on 9 December 2025. Its attachment is not treated as the universal Preliminary syllabus for future BCS examinations.

5. **50th BCS Preliminary syllabus attachment**  
   https://objectstorage.ap-dcc-gazipur-1.oraclecloud15.com/n/axvjbnqprylg/b/V2Ministry/o/office-bpsc/2024/12/be22c1ddda60414fad98b0a82080ef9a.pdf  
   This exam-specific attachment is recorded as a source reference but is not applied to the common Preliminary version.

## Seeded, source-verified outlines

### Preliminary

BPSC states the test is 200 marks and lists these subject groups and marks:

| Official Bengali subject group | Marks |
| --- | ---: |
| বাংলা ভাষা ও সাহিত্য | 35 |
| ইংরেজি ভাষা ও সাহিত্য | 35 |
| বাংলাদেশ বিষয়াবলি | 30 |
| আন্তর্জাতিক বিষয়াবলি | 20 |
| ভূগোল (বাংলাদেশ ও বিশ্ব), পরিবেশ ও দুর্যোগ ব্যবস্থাপনা | 10 |
| সাধারণ বিজ্ঞান | 15 |
| কম্পিউটার ও তথ্য প্রযুক্তি | 15 |
| গাণিতিক যুক্তি | 15 |
| মানসিক দক্ষতা | 15 |
| নৈতিকতা, মূল্যবোধ ও সুশাসন | 10 |

### Written

BPSC states the written examination is 900 marks. The seeded versions represent the subject/mark outlines for the two published candidate tracks:

| General-cadre subject | Marks |
| --- | ---: |
| বাংলা | 200 |
| ইংরেজি | 200 |
| বাংলাদেশ বিষয়াবলি | 200 |
| আন্তর্জাতিক বিষয়াবলি | 100 |
| গাণিতিক যুক্তি ও মানসিক দক্ষতা | 100 |
| সাধারণ বিজ্ঞান ও প্রযুক্তি | 100 |

| Technical/professional subject | Marks |
| --- | ---: |
| বাংলা | 100 |
| ইংরেজি | 200 |
| বাংলাদেশ বিষয়াবলি | 200 |
| আন্তর্জাতিক বিষয়াবলি | 100 |
| গাণিতিক যুক্তি ও মানসিক দক্ষতা | 100 |
| পদ-সংশ্লিষ্ট বিষয় | 200 |

The BPSC page additionally says candidates opting for both general and technical/professional cadres sit an additional 200-mark job-related examination. The seeded technical/professional outline keeps that subject generic; it does not invent a cadre-specific paper or topic.

### Viva

BPSC states that the oral examination is 200 marks and the pass mark is 50%. The source page does not provide a topic-by-topic Viva syllabus. The published version therefore has no subject/topic entries and explicitly says this in its summary.

## Updating source status

When a detailed official PDF is reviewed and its applicability/date are confirmed, create a new draft version, attach the precise official source, add topics with item-level references, mark only verified items as `Verified`, and publish after review. Do not edit an already published version; archive it and publish a new version instead.
