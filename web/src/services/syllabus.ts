import { ApiClient, type PagedResult } from './api'

export interface ExamStage {
  id: string
  code: string
  name: string
  localizedName: string
  sortOrder: number
  isActive: boolean
}

export interface SyllabusVersion {
  id: string
  examStageId: string
  stageCode: string
  stageName: string
  versionLabel: string
  trackCode: string
  summary: string
  coverage: 'OutlineOnly' | 'FullTopics'
  status: 'Draft' | 'Published' | 'Archived'
  effectiveFrom: string | null
  effectiveTo: string | null
  publishedAt: string | null
  subjectCount: number
  sourceCount: number
}

export interface SyllabusPaper {
  id: string
  syllabusSubjectId: string
  code: string
  name: string
  sortOrder: number
  maxMarks: number | null
}

export interface SyllabusItem {
  id: string
  syllabusSubjectId: string
  subjectId: string
  paperId: string | null
  parentId: string | null
  kind: 'Topic' | 'Subtopic'
  title: string
  description: string | null
  sortOrder: number
  verificationStatus: 'Unverified' | 'Verified' | 'NeedsReview'
  isCompleted: boolean | null
  children: SyllabusItem[]
}

export interface SyllabusSubject {
  id: string
  subjectId: string
  subjectCode: string
  subjectName: string
  sortOrder: number
  marks: number | null
  isRequired: boolean
  papers: SyllabusPaper[]
  items: SyllabusItem[]
}

export interface SyllabusSource {
  id: string
  title: string
  publisher: string
  url: string
  sourceType: string
  publishedOn: string | null
  lastVerifiedOn: string | null
  verificationStatus: 'Unverified' | 'Verified' | 'Superseded'
  notes: string | null
}

export interface SyllabusTree {
  version: SyllabusVersion
  subjects: SyllabusSubject[]
  sources: SyllabusSource[]
}

export interface SyllabusSearchHit {
  id: string
  versionId: string
  stageCode: string
  stageName: string
  versionLabel: string
  subjectId: string
  subjectName: string
  kind: 'Topic' | 'Subtopic'
  title: string
  description: string | null
  verificationStatus: 'Unverified' | 'Verified' | 'NeedsReview'
}

export interface SyllabusProgress {
  versionId: string
  totalItems: number
  completedItems: number
  items: Record<string, boolean>
}

export interface SubjectRecord {
  id: string
  code: string
  name: string
  description: string | null
  isActive: boolean
}

export interface SyllabusItemRecord extends SyllabusItem {
  children: []
}

export interface PageQuery {
  pageNumber?: number
  pageSize?: number
  sortBy?: string
}

export interface VersionQuery extends PageQuery {
  stageCode?: string
  trackCode?: string
  status?: string
}

export interface VersionInput {
  versionLabel: string
  trackCode: string
  summary: string
  coverage: 'OutlineOnly' | 'FullTopics'
  effectiveFrom: string | null
  effectiveTo: string | null
}

export interface SubjectInput {
  code: string
  name: string
  description: string | null
}

export interface SyllabusSubjectInput {
  subjectId: string
  sortOrder: number
  marks: number | null
  isRequired: boolean
}

export interface PaperInput {
  code: string
  name: string
  sortOrder: number
  maxMarks: number | null
}

export interface SyllabusItemInput {
  syllabusSubjectId: string
  paperId: string | null
  parentId: string | null
  title: string
  description: string | null
  sortOrder: number
  verificationStatus: 'Unverified' | 'Verified' | 'NeedsReview'
}

export interface SyllabusSourceInput {
  title: string
  publisher: string
  url: string
  sourceType: string
  publishedOn: string | null
  lastVerifiedOn: string | null
  verificationStatus: 'Unverified' | 'Verified' | 'Superseded'
  notes: string | null
}

function queryString(params: object) {
  const search = new URLSearchParams()
  Object.entries(params as Record<string, string | number | boolean | undefined | null>).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value))
  })
  return search.toString()
}

class SyllabusApi extends ApiClient {
  getStages() {
    return this.get<ExamStage[]>('/syllabus/stages')
  }

  getVersions(query: VersionQuery = {}) {
    return this.get<PagedResult<SyllabusVersion>>(`/syllabus/versions?${queryString(query)}`)
  }

  getTree(versionId: string, token?: string) {
    return this.get<SyllabusTree>(`/syllabus/versions/${versionId}/tree`, token)
  }

  getItemSources(itemId: string) {
    return this.get<SyllabusSource[]>(`/syllabus/items/${itemId}/sources`)
  }

  search(params: {
    q: string
    stageCode?: string
    versionId?: string
    subjectId?: string
    kind?: string
    pageNumber?: number
    pageSize?: number
    sortBy?: string
  }) {
    return this.get<PagedResult<SyllabusSearchHit>>(`/syllabus/search?${queryString(params)}`)
  }

  getProgress(versionId: string, token: string) {
    return this.get<SyllabusProgress>(`/me/topic-progress?versionId=${encodeURIComponent(versionId)}`, token)
  }

  setProgress(itemId: string, isCompleted: boolean, token: string) {
    return this.put<void>(`/me/topic-progress/${itemId}`, { isCompleted }, token)
  }

  getAdminVersions(query: VersionQuery, token: string) {
    return this.get<PagedResult<SyllabusVersion>>(`/admin/syllabus/versions?${queryString(query)}`, token)
  }

  createVersion(stageId: string, input: VersionInput, token: string) {
    return this.post<SyllabusVersion>(`/admin/syllabus/stages/${stageId}/versions`, input, token)
  }

  updateVersion(id: string, input: VersionInput, token: string) {
    return this.put<SyllabusVersion>(`/admin/syllabus/versions/${id}`, input, token)
  }

  publishVersion(id: string, token: string) {
    return this.post<SyllabusVersion>(`/admin/syllabus/versions/${id}/publish`, {}, token)
  }

  archiveVersion(id: string, token: string) {
    return this.post<SyllabusVersion>(`/admin/syllabus/versions/${id}/archive`, {}, token)
  }

  getAdminTree(id: string, token: string) {
    return this.get<SyllabusTree>(`/admin/syllabus/versions/${id}/tree`, token)
  }

  getAdminStages(token: string) {
    return this.get<ExamStage[]>('/admin/syllabus/stages', token)
  }

  updateStage(id: string, input: Pick<ExamStage, 'name' | 'localizedName' | 'sortOrder' | 'isActive'>, token: string) {
    return this.put<void>(`/admin/syllabus/stages/${id}`, input, token)
  }

  getSubjects(token: string, search = '') {
    return this.get<PagedResult<SubjectRecord>>(
      `/admin/syllabus/subjects?${queryString({ search, includeInactive: true, pageSize: 100, sortBy: 'name' })}`,
      token,
    )
  }

  createSubject(input: SubjectInput, token: string) {
    return this.post<SubjectRecord>('/admin/syllabus/subjects', input, token)
  }

  updateSubject(id: string, input: Pick<SubjectRecord, 'name' | 'description' | 'isActive'>, token: string) {
    return this.put<SubjectRecord>(`/admin/syllabus/subjects/${id}`, input, token)
  }

  assignSubject(versionId: string, input: SyllabusSubjectInput, token: string) {
    return this.post<SyllabusSubject>(`/admin/syllabus/versions/${versionId}/subjects`, input, token)
  }

  updateSyllabusSubject(id: string, input: Omit<SyllabusSubjectInput, 'subjectId'>, token: string) {
    return this.put<SyllabusSubject>(`/admin/syllabus/syllabus-subjects/${id}`, input, token)
  }

  getAdminPapers(syllabusSubjectId: string, token: string) {
    return this.get<SyllabusPaper[]>(`/admin/syllabus/papers?syllabusSubjectId=${encodeURIComponent(syllabusSubjectId)}`, token)
  }

  createPaper(syllabusSubjectId: string, input: PaperInput, token: string) {
    return this.post<SyllabusPaper>(`/admin/syllabus/syllabus-subjects/${syllabusSubjectId}/papers`, input, token)
  }

  updatePaper(id: string, input: Omit<PaperInput, 'code'>, token: string) {
    return this.put<SyllabusPaper>(`/admin/syllabus/papers/${id}`, input, token)
  }

  deletePaper(id: string, token: string) {
    return this.delete(`/admin/syllabus/papers/${id}`, token)
  }

  getAdminItems(versionId: string, token: string) {
    return this.get<PagedResult<SyllabusItemRecord>>(
      `/admin/syllabus/items?${queryString({ versionId, pageSize: 100 })}`,
      token,
    )
  }

  createItem(kind: 'topics' | 'subtopics', input: SyllabusItemInput, token: string) {
    return this.post<SyllabusItem>(`/admin/syllabus/${kind}`, input, token)
  }

  updateItem(id: string, input: Omit<SyllabusItemInput, 'syllabusSubjectId'>, token: string) {
    return this.put<SyllabusItem>(`/admin/syllabus/items/${id}`, input, token)
  }

  deleteItem(id: string, token: string) {
    return this.delete(`/admin/syllabus/items/${id}`, token)
  }

  getSources(versionId: string, token: string) {
    return this.get<SyllabusSource[]>(`/admin/syllabus/versions/${versionId}/sources`, token)
  }

  getSource(id: string, token: string) {
    return this.get<SyllabusSource>(`/admin/syllabus/sources/${id}`, token)
  }

  createSource(input: SyllabusSourceInput, token: string) {
    return this.post<SyllabusSource>('/admin/syllabus/sources', input, token)
  }

  updateSource(id: string, input: SyllabusSourceInput, token: string) {
    return this.put<SyllabusSource>(`/admin/syllabus/sources/${id}`, input, token)
  }

  linkVersionSource(versionId: string, sourceId: string, notes: string, token: string) {
    return this.put<void>(`/admin/syllabus/versions/${versionId}/sources/${sourceId}`, { notes }, token)
  }

  unlinkVersionSource(versionId: string, sourceId: string, token: string) {
    return this.delete(`/admin/syllabus/versions/${versionId}/sources/${sourceId}`, token)
  }

  linkItemSource(itemId: string, sourceId: string, token: string) {
    return this.put<void>(`/admin/syllabus/items/${itemId}/sources/${sourceId}`, {}, token)
  }

  unlinkItemSource(itemId: string, sourceId: string, token: string) {
    return this.delete(`/admin/syllabus/items/${itemId}/sources/${sourceId}`, token)
  }
}

export const syllabusApi = new SyllabusApi()
