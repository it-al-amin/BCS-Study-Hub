import { ApiClient, type PagedResult } from './api'

export const currentAffairsCategories = [
  'Bangladesh', 'InternationalAffairs', 'Economy', 'Government', 'Politics', 'Constitution', 'Diplomacy',
  'InternationalOrganizations', 'Science', 'Technology', 'Environment', 'ClimateChange', 'Awards', 'Sports',
  'Appointments', 'GovernmentPolicies', 'NationalProjects', 'Reports', 'ImportantEvents',
] as const
export type CurrentAffairsCategory = (typeof currentAffairsCategories)[number]
export type CurrentAffairsStatus = 'Draft' | 'Published' | 'Archived'
export type DigestPeriod = 'Day' | 'Week' | 'Month' | 'Year'
export type QuestionType = 'Mcq' | 'Written' | 'Viva'
export type ReferenceKind = 'Pdf' | 'Image' | 'WebPage'
export type RevisionOutcome = 'Again' | 'Hard' | 'Good' | 'Easy'

export const officialSourceCategories = [
  'ConstitutionalBody', 'GovernmentPortal', 'Ministry', 'Department', 'CentralBank', 'Statistics', 'Parliament',
  'LegalAndLegislative', 'Judiciary', 'OfficialNews', 'InternationalOrganization', 'ReportsAndPublications',
] as const
export type OfficialSourceCategory = (typeof officialSourceCategories)[number]
export type VerificationStatus = 'Unverified' | 'Verified' | 'NeedsReview' | 'Unreachable'

export interface CurrentAffairsSummary {
  id: string
  title: string
  summary: string
  eventDate: string
  sourcePublishedOn: string | null
  category: CurrentAffairsCategory
  status: CurrentAffairsStatus
  sourceName: string
  sourceUrl: string
  sourceStatus: 'Unverified' | 'Verified'
  tags: string[]
  subjects: { id: string; code: string; name: string }[]
  topics: { id: string; title: string; subjectName: string; stageCode: string; versionLabel: string }[]
  isBookmarked: boolean
  inRevision: boolean
  hasNote: boolean
  updatedAt: string
  publishedAt: string | null
}

export interface CurrentAffairsNote { entryId: string; entryTitle: string; eventDate: string; content: string; updatedAt: string }

export interface CurrentAffairsRevision {
  entryId: string
  entryTitle: string
  category: CurrentAffairsCategory
  eventDate: string
  reviewCount: number
  intervalDays: number
  nextReviewOn: string
  lastReviewedAt: string | null
  lastOutcome: RevisionOutcome | null
  isDue: boolean
}

export interface CurrentAffairsDetail {
  entry: CurrentAffairsSummary
  content: string | null
  notes: string | null
  officialSource: { id: string; name: string; url: string; verificationStatus: VerificationStatus; lastVerifiedOn: string | null } | null
  questions: { type: QuestionType; id: string; prompt: string; status: string; isAvailable: boolean }[]
  references: { id: string; kind: ReferenceKind; title: string; url: string }[]
  libraryResources: { id: string; title: string; kind: string; primaryFormat: string | null; isAvailable: boolean }[]
  myNote: CurrentAffairsNote | null
  myRevision: CurrentAffairsRevision | null
  createdAt: string
}

export interface CurrentAffairsDigest {
  period: DigestPeriod
  label: string
  from: string
  to: string
  previousAnchor: string
  nextAnchor: string
  total: number
  groups: { category: CurrentAffairsCategory; entries: CurrentAffairsSummary[] }[]
}

export interface CurrentAffairsQuery {
  search?: string
  category?: string
  from?: string
  to?: string
  tag?: string
  subjectId?: string
  syllabusItemId?: string
  bookmarkedOnly?: boolean
  pageNumber?: number
  pageSize?: number
  sortBy?: 'recent' | 'oldest' | 'updated'
}

export interface CurrentAffairsEntryInput {
  title: string
  summary: string
  content: string | null
  eventDate: string
  sourcePublishedOn: string | null
  category: CurrentAffairsCategory
  sourceName: string
  sourceUrl: string
  officialSourceId: string | null
  sourceStatus: 'Unverified' | 'Verified'
  notes: string | null
  tags: string[]
  subjectIds: string[]
  syllabusItemIds: string[]
  questions: { type: QuestionType; questionId: string }[]
  references: { kind: ReferenceKind; title: string; url: string }[]
  libraryResourceIds: string[]
}

export interface OfficialSource {
  id: string
  name: string
  localizedName: string | null
  category: OfficialSourceCategory
  url: string
  isHttps: boolean
  description: string | null
  parentOrganization: string | null
  isInternational: boolean
  usageNotes: string | null
  verificationStatus: VerificationStatus
  lastVerifiedOn: string | null
  verificationNote: string | null
  status: CurrentAffairsStatus
  sortOrder: number
  tags: string[]
  updatedAt: string
}

export interface OfficialSourceInput {
  name: string
  localizedName: string | null
  category: OfficialSourceCategory
  url: string
  description: string | null
  parentOrganization: string | null
  isInternational: boolean
  usageNotes: string | null
  sortOrder: number
  tags: string[]
}

function queryString(params: object) {
  const search = new URLSearchParams()
  Object.entries(params as Record<string, string | number | boolean | undefined | null>).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '' && value !== false) search.set(key, String(value))
  })
  return search.toString()
}

const encode = encodeURIComponent

class CurrentAffairsApi extends ApiClient {
  getEntries(query: CurrentAffairsQuery, token?: string | null) {
    return this.get<PagedResult<CurrentAffairsSummary>>(`/current-affairs/entries?${queryString(query)}`, token ?? undefined)
  }

  getEntry(id: string, token?: string | null) {
    return this.get<CurrentAffairsDetail>(`/current-affairs/entries/${encode(id)}`, token ?? undefined)
  }

  getDigest(period: DigestPeriod, date: string | undefined, category: string | undefined, token?: string | null) {
    return this.get<CurrentAffairsDigest>(`/current-affairs/digest?${queryString({ period, date, category })}`, token ?? undefined)
  }

  getArchive() {
    return this.get<{ year: number; month: number; count: number }[]>('/current-affairs/archive')
  }

  getCategories() {
    return this.get<{ category: CurrentAffairsCategory; count: number }[]>('/current-affairs/categories')
  }

  getTags() {
    return this.get<string[]>('/current-affairs/tags')
  }

  addBookmark(id: string, token: string) { return this.post<void>(`/me/current-affairs/entries/${encode(id)}/bookmark`, {}, token) }
  removeBookmark(id: string, token: string) { return this.delete(`/me/current-affairs/entries/${encode(id)}/bookmark`, token) }
  getBookmarks(pageNumber: number, token: string) {
    return this.get<PagedResult<{ entryId: string; title: string; category: CurrentAffairsCategory; eventDate: string; createdAt: string }>>(`/me/current-affairs/bookmarks?pageNumber=${pageNumber}&pageSize=20`, token)
  }
  getNotes(pageNumber: number, token: string) { return this.get<PagedResult<CurrentAffairsNote>>(`/me/current-affairs/notes?pageNumber=${pageNumber}&pageSize=20`, token) }
  saveNote(id: string, content: string, token: string) { return this.put<CurrentAffairsNote>(`/me/current-affairs/entries/${encode(id)}/note`, { content }, token) }
  deleteNote(id: string, token: string) { return this.delete(`/me/current-affairs/entries/${encode(id)}/note`, token) }
  getRevisions(dueOnly: boolean, pageNumber: number, token: string) {
    return this.get<PagedResult<CurrentAffairsRevision>>(`/me/current-affairs/revision?${queryString({ dueOnly, pageNumber, pageSize: 20 })}`, token)
  }
  addRevision(id: string, token: string) { return this.post<CurrentAffairsRevision>(`/me/current-affairs/entries/${encode(id)}/revision`, {}, token) }
  review(id: string, outcome: RevisionOutcome, token: string) { return this.post<CurrentAffairsRevision>(`/me/current-affairs/entries/${encode(id)}/revision/review`, { outcome }, token) }
  removeRevision(id: string, token: string) { return this.delete(`/me/current-affairs/entries/${encode(id)}/revision`, token) }

  getAdminEntries(query: { search?: string; status?: string; category?: string; pageNumber?: number }, token: string) {
    return this.get<PagedResult<CurrentAffairsSummary>>(`/admin/current-affairs/entries?${queryString({ ...query, pageSize: 25 })}`, token)
  }
  getAdminEntry(id: string, token: string) { return this.get<CurrentAffairsDetail>(`/admin/current-affairs/entries/${encode(id)}`, token) }
  createEntry(input: CurrentAffairsEntryInput, token: string) { return this.post<CurrentAffairsDetail>('/admin/current-affairs/entries', input, token) }
  updateEntry(id: string, input: CurrentAffairsEntryInput, token: string) { return this.put<CurrentAffairsDetail>(`/admin/current-affairs/entries/${encode(id)}`, input, token) }
  setEntryStatus(id: string, action: 'publish' | 'archive', token: string) { return this.post<CurrentAffairsDetail>(`/admin/current-affairs/entries/${encode(id)}/${action}`, {}, token) }
  deleteEntry(id: string, token: string) { return this.delete(`/admin/current-affairs/entries/${encode(id)}`, token) }
  searchQuestions(type: QuestionType | '', search: string, token: string) {
    return this.get<{ type: QuestionType; id: string; prompt: string; status: string }[]>(`/admin/current-affairs/question-search?${queryString({ type, search })}`, token)
  }
  searchResources(search: string, token: string) {
    return this.get<{ id: string; title: string; kind: string; primaryFormat: string | null }[]>(`/admin/current-affairs/resource-search?${queryString({ search })}`, token)
  }

  getOfficialSources(query: { search?: string; category?: string }) {
    return this.get<OfficialSource[]>(`/official-sources?${queryString(query)}`)
  }
  getAdminSources(query: { search?: string; status?: string; verificationStatus?: string }, token: string) {
    return this.get<PagedResult<OfficialSource>>(`/admin/official-sources?${queryString({ ...query, pageSize: 100 })}`, token)
  }
  createSource(input: OfficialSourceInput, token: string) { return this.post<OfficialSource>('/admin/official-sources', input, token) }
  updateSource(id: string, input: OfficialSourceInput, token: string) { return this.put<OfficialSource>(`/admin/official-sources/${encode(id)}`, input, token) }
  recordVerification(id: string, status: Exclude<VerificationStatus, 'Unverified'>, note: string, token: string) {
    return this.post<OfficialSource>(`/admin/official-sources/${encode(id)}/verification`, { status, note }, token)
  }
  setSourceStatus(id: string, action: 'publish' | 'archive', token: string) { return this.post<OfficialSource>(`/admin/official-sources/${encode(id)}/${action}`, {}, token) }
  deleteSource(id: string, token: string) { return this.delete(`/admin/official-sources/${encode(id)}`, token) }
}

export const currentAffairsApi = new CurrentAffairsApi()
