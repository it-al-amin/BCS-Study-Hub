# Official resource directory: source register

## Verification method

On 2026-11-21 the landing page of each candidate URL was fetched once. A source was recorded as **Verified** only when the fetched page itself identified the organisation (page title, heading, or footer). No page beyond the landing page was crawled, and no content was copied into the database; the directory stores names, URLs, and short descriptions only.

Sources whose check failed were seeded as **unpublished drafts** with the failure recorded. An editor must open them in a browser and record the outcome (Admin → Official sources → verify) before they can be published. Failures from an automated client (certificate-chain or bot-blocking errors) do not mean a site is not official; they mean it was not confirmed.

The server never fetches these URLs itself. Verification is always a manual, audited editor action, which avoids scraping and server-side request forgery. Changing a URL voids its verification and unpublishes the entry. A source cannot be published with a verification older than 365 days.

## Seeded as verified and published

| Category | Organisation | URL | Evidence on page |
| --- | --- | --- | --- |
| Constitutional body | Bangladesh Public Service Commission | https://bpsc.gov.bd/ | Title "হোম \| বাংলাদেশ সরকারী কর্ম কমিশন" |
| Government portal | Bangladesh National Portal | https://bangladesh.gov.bd/ | Title "হোম \| বাংলাদেশ জাতীয় তথ্য বাতায়ন" |
| Ministry / division | Cabinet Division | https://cabinet.gov.bd/ | Title "হোম \| মন্ত্রিপরিষদ বিভাগ" |
| Ministry / division | Ministry of Public Administration | https://mopa.gov.bd/ | Title "হোম \| জনপ্রশাসন মন্ত্রণালয়" |
| Ministry / division | Finance Division, Ministry of Finance | https://mof.gov.bd/ | Title "হোম \| অর্থ বিভাগ, অর্থ মন্ত্রণালয়" |
| Ministry / division | Economic Relations Division | https://erd.gov.bd/ | Title "হোম \| অর্থনৈতিক সম্পর্ক বিভাগ" |
| Ministry / division | Ministry of Foreign Affairs | https://mofa.gov.bd/ | Title "হোম \| পররাষ্ট্র মন্ত্রণালয়" |
| Ministry / division | Ministry of Environment, Forest and Climate Change | https://moef.gov.bd/ | Title names the ministry |
| Ministry / division | Ministry of Education | https://moedu.gov.bd/ | Title "হোম \| শিক্ষা মন্ত্রণালয়" |
| Department | Department of Printing and Publications | https://www.dpp.gov.bd/ | Title "Department of Printing and Publications". The page says BG Press (Bangladesh Gazette) is a DPP unit and that the current site is dpp.portal.gov.bd, which was **not** separately verified. |
| Central bank | Bangladesh Bank | https://www.bb.org.bd/ | Header "Bangladesh Bank", Motijheel address |
| Legal and legislative | Laws of Bangladesh | http://bdlaws.minlaw.gov.bd/ | Title "Laws of Bangladesh"; footer credits the Legislative and Parliamentary Affairs Division. **Served over HTTP only**, so the UI flags it. |
| Legal and legislative | Legislative and Parliamentary Affairs Division | https://legislativediv.gov.bd/ | Title names the division |
| Judiciary | Supreme Court of Bangladesh | https://www.supremecourt.gov.bd/ | Title "Home : Supreme Court of Bangladesh" |
| Official news | Bangladesh Sangbad Sangstha (BSS) | https://www.bssnews.net/ | Title "Bangladesh Sangbad Sangstha (BSS)" |
| International | United Nations in Bangladesh | https://bangladesh.un.org/ | Header "United Nations in Bangladesh" |
| International | World Bank in Bangladesh | https://www.worldbank.org/en/country/bangladesh | Title "Bangladesh \| World Bank" |
| International | Asian Development Bank – Bangladesh | https://www.adb.org/countries/bangladesh/main | Title "Bangladesh and ADB \| Asian Development Bank" |
| International | International Monetary Fund – Bangladesh | https://www.imf.org/en/Countries/BGD | Title "Bangladesh and the IMF" |

## Seeded as drafts: verification required

| Organisation | URL | Automated result |
| --- | --- | --- |
| Planning Commission | https://plancomm.gov.bd/ | TLS certificate could not be verified |
| Bangladesh Bureau of Statistics | https://bbs.gov.bd/ | TLS certificate could not be verified |
| Bangladesh Parliament (Jatiya Sangsad) | https://www.parliament.gov.bd/ | TLS certificate could not be verified |
| Press Information Department | https://pressinform.gov.bd/ | TLS certificate could not be verified |
| Bangladesh Election Commission | https://www.ecs.gov.bd/ | HTTP 403 |
| United Nations | https://www.un.org/ | HTTP 403 |
| UNDP Bangladesh | https://www.undp.org/bangladesh | HTTP 403 |

## Content and copyright policy

- **No scraping.** Current-affairs entries are written by editors from a cited source, summarised in the editor's own words, with a link to the original. No feed, crawler, or automated import exists.
- **Respect terms of service.** The directory links to official sites; it does not mirror their pages, documents, or data. Each entry carries a usage note reminding learners to check the publisher's terms before reusing material.
- **Files** attached to current-affairs entries must be shared, published digital-library resources, which already passed the library's rights-basis checks ([LIBRARY_SECURITY_REVIEW.md](LIBRARY_SECURITY_REVIEW.md)). Otherwise entries link to PDFs and images hosted by the publisher.
- **Publication gate.** An entry can only be published after the editor confirms they opened the cited source and it supports the entry (`SourceStatus = Verified`). Every publish, archive, and verification is audited.
- **No current-affairs content is seeded.** Only the directory above is.
