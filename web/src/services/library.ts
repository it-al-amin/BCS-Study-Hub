import { ApiClient, type PagedResult } from './api'

export type LibraryResourceKind = 'Book' | 'File' | 'ExternalLink' | 'Note'
export type LibraryResourceCategory =
  | 'General'
  | 'Textbook'
  | 'StudyNote'
  | 'GovernmentPublication'
  | 'Report'
  | 'NewspaperArticle'
  | 'Reference'
  | 'ChapterMaterial'
  | 'Other'
export type LibraryVisibility = 'Private' | 'Shared'
export type LibraryResourceStatus = 'Draft' | 'Published' | 'Archived'
export type LibrarySourceType =
  | 'Unspecified'
  | 'OfficialGovernment'
  | 'Academic'
  | 'NewsMedia'
  | 'ReferenceWork'
  | 'Commercial'
  | 'Personal'
  | 'Other'
export type LibraryRightsBasis =
  | 'NotApplicable'
  | 'PersonalCopy'
  | 'OwnWork'
  | 'PublicDomain'
  | 'GovernmentPublication'
  | 'OpenLicense'
  | 'PermissionGranted'
export type LibraryFileRole = 'Primary' | 'Cover'
export type LibraryFileFormat = 'Pdf' | 'Image' | 'Document' | 'Text'
export type LibraryReadingStatus = 'NotStarted' | 'Reading' | 'OnHold' | 'Completed'
export type LibraryMode = 'personal' | 'admin'

export interface LibrarySubjectRef { id: string; code: string; name: string }
export interface LibraryTopicRef {
  id: string
  title: string
  kind: 'Topic' | 'Subtopic'
  subjectId: string
  subjectName: string
  syllabusVersionId: string
  versionLabel: string
  stageCode: string
}
export interface LibraryResourceRef { id: string; title: string; kind: LibraryResourceKind }

export interface LibraryBook {
  publisher: string | null
  edition: string | null
  publicationYear: number | null
  isbn: string | null
  pageCount: number | null
  language: string | null
}

export interface LibraryFile {
  id: string
  role: LibraryFileRole
  format: LibraryFileFormat
  originalFileName: string
  contentType: string
  sizeBytes: number
  sha256: string
  uploadedAt: string
  canPreviewInline: boolean
}

export interface LibraryProgress {
  resourceId: string
  resourceTitle: string
  resourceKind: LibraryResourceKind
  status: LibraryReadingStatus
  currentChapterId: string | null
  currentChapterTitle: string | null
  currentPage: number | null
  totalPages: number | null
  percentComplete: number
  startedAt: string | null
  completedAt: string | null
  updatedAt: string
}

export interface LibraryResourceSummary {
  id: string
  kind: LibraryResourceKind
  category: LibraryResourceCategory
  visibility: LibraryVisibility
  status: LibraryResourceStatus
  title: string
  description: string | null
  authors: string | null
  sourceName: string | null
  sourceType: LibrarySourceType
  url: string | null
  publicationDate: string | null
  book: LibraryBook | null
  primaryFormat: LibraryFileFormat | null
  primaryFileId: string | null
  coverFileId: string | null
  chapterCount: number
  subjects: LibrarySubjectRef[]
  topics: LibraryTopicRef[]
  tags: string[]
  isOwner: boolean
  isBookmarked: boolean
  progress: LibraryProgress | null
  createdAt: string
  updatedAt: string
}

export interface LibraryChapter {
  id: string
  chapterNumber: number
  title: string
  summary: string | null
  startPage: number | null
  endPage: number | null
  topics: LibraryTopicRef[]
  linkedResources: LibraryResourceRef[]
}

export interface LibraryBookmark {
  id: string
  resourceId: string
  resourceTitle: string
  resourceKind: LibraryResourceKind
  chapterId: string | null
  chapterTitle: string | null
  page: number | null
  label: string | null
  createdAt: string
}

export interface LibraryNote {
  id: string
  resourceId: string
  resourceTitle: string
  chapterId: string | null
  chapterTitle: string | null
  page: number | null
  content: string
  createdAt: string
  updatedAt: string
}

export interface LibraryResourceDetail {
  resource: LibraryResourceSummary
  website: string | null
  rightsBasis: LibraryRightsBasis
  rightsNote: string | null
  noteContent: string | null
  publishedAt: string | null
  canEdit: boolean
  files: LibraryFile[]
  chapters: LibraryChapter[]
  bookmarks: LibraryBookmark[]
  notes: LibraryNote[]
  usedInChapters: LibraryResourceRef[]
}

export interface LibraryBookInput {
  publisher: string | null
  edition: string | null
  publicationYear: number | null
  isbn: string | null
  pageCount: number | null
  language: string | null
}

export interface LibraryResourceInput {
  kind: LibraryResourceKind
  category: LibraryResourceCategory
  title: string
  description: string | null
  authors: string | null
  sourceName: string | null
  sourceType: LibrarySourceType
  website: string | null
  url: string | null
  publicationDate: string | null
  rightsBasis: LibraryRightsBasis
  rightsNote: string | null
  noteContent: string | null
  book: LibraryBookInput | null
  subjectIds: string[]
  syllabusItemIds: string[]
  tags: string[]
}

export interface LibraryChapterInput {
  chapterNumber: number
  title: string
  summary: string | null
  startPage: number | null
  endPage: number | null
  syllabusItemIds: string[]
  linkedResourceIds: string[]
}

export interface LibraryProgressInput {
  status: LibraryReadingStatus
  currentChapterId: string | null
  currentPage: number | null
  totalPages: number | null
  percentComplete: number | null
}

export interface LibraryCollection {
  id: string
  name: string
  description: string | null
  itemCount: number
  createdAt: string
  updatedAt: string
}

export interface LibraryCollectionDetail {
  collection: LibraryCollection
  items: LibraryResourceSummary[]
}

export interface LibraryStorageUsage {
  usedBytes: number
  quotaBytes: number
  maxFileSizeBytes: number
  allowedExtensions: string[]
}

export interface LibraryDashboard {
  visibleResources: number
  sharedResources: number
  privateResources: number
  byKind: { kind: LibraryResourceKind; count: number }[]
  bookmarkCount: number
  collectionCount: number
  readingCount: number
  completedCount: number
  storage: LibraryStorageUsage
  continueReading: LibraryProgress[]
  recentlyAdded: LibraryResourceSummary[]
  recentBookmarks: LibraryBookmark[]
}

export interface LibraryResourceQuery {
  search?: string
  kind?: LibraryResourceKind | ''
  category?: LibraryResourceCategory | ''
  format?: LibraryFileFormat | ''
  subjectId?: string
  syllabusItemId?: string
  tag?: string
  scope?: 'all' | 'shared' | 'mine'
  bookmarkedOnly?: boolean
  pageNumber?: number
  pageSize?: number
  sortBy?: 'recent' | 'added' | 'title' | 'published'
}

function queryString(params: object) {
  const search = new URLSearchParams()
  Object.entries(params as Record<string, string | number | boolean | undefined | null>).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '' && value !== false) search.set(key, String(value))
  })
  return search.toString()
}

const encode = encodeURIComponent

class LibraryApi extends ApiClient {
  getDashboard(token: string) {
    return this.get<LibraryDashboard>('/library/dashboard', token)
  }

  getResources(query: LibraryResourceQuery, token: string) {
    return this.get<PagedResult<LibraryResourceSummary>>(`/library/resources?${queryString(query)}`, token)
  }

  getResource(id: string, token: string) {
    return this.get<LibraryResourceDetail>(`/library/resources/${encode(id)}`, token)
  }

  getTags(token: string) {
    return this.get<string[]>('/library/tags', token)
  }

  /** Fetches a protected file with the bearer token so it can be shown through an object URL. */
  getFileBlob(resourceId: string, fileId: string, token: string, mode: LibraryMode = 'personal') {
    const base = mode === 'admin' ? '/admin/library' : '/library'
    return this.getBlob(`${base}/resources/${encode(resourceId)}/files/${encode(fileId)}/content`, token)
  }

  downloadFileBlob(resourceId: string, fileId: string, token: string, mode: LibraryMode = 'personal') {
    const base = mode === 'admin' ? '/admin/library' : '/library'
    return this.getBlob(`${base}/resources/${encode(resourceId)}/files/${encode(fileId)}/content?download=true`, token)
  }

  // Management: personal resources live under /me/library, shared ones under /admin/library.
  getManagedResource(mode: LibraryMode, id: string, token: string) {
    return this.get<LibraryResourceDetail>(`${managePath(mode)}/resources/${encode(id)}`, token)
  }

  createResource(mode: LibraryMode, input: LibraryResourceInput, token: string) {
    return this.post<LibraryResourceDetail>(`${managePath(mode)}/resources`, input, token)
  }

  updateResource(mode: LibraryMode, id: string, input: LibraryResourceInput, token: string) {
    return this.put<LibraryResourceDetail>(`${managePath(mode)}/resources/${encode(id)}`, input, token)
  }

  deleteResource(mode: LibraryMode, id: string, token: string) {
    return this.delete(`${managePath(mode)}/resources/${encode(id)}`, token)
  }

  uploadFile(mode: LibraryMode, resourceId: string, role: LibraryFileRole, file: File, token: string) {
    const form = new FormData()
    form.append('file', file)
    return this.postForm<LibraryFile>(`${managePath(mode)}/resources/${encode(resourceId)}/files?role=${role}`, form, token)
  }

  deleteFile(mode: LibraryMode, resourceId: string, fileId: string, token: string) {
    return this.delete(`${managePath(mode)}/resources/${encode(resourceId)}/files/${encode(fileId)}`, token)
  }

  addChapter(mode: LibraryMode, resourceId: string, input: LibraryChapterInput, token: string) {
    return this.post<LibraryChapter>(`${managePath(mode)}/resources/${encode(resourceId)}/chapters`, input, token)
  }

  updateChapter(mode: LibraryMode, resourceId: string, chapterId: string, input: LibraryChapterInput, token: string) {
    return this.put<LibraryChapter>(`${managePath(mode)}/resources/${encode(resourceId)}/chapters/${encode(chapterId)}`, input, token)
  }

  deleteChapter(mode: LibraryMode, resourceId: string, chapterId: string, token: string) {
    return this.delete(`${managePath(mode)}/resources/${encode(resourceId)}/chapters/${encode(chapterId)}`, token)
  }

  getAdminResources(query: { search?: string; status?: string; kind?: string; pageNumber?: number; pageSize?: number }, token: string) {
    return this.get<PagedResult<LibraryResourceSummary>>(`/admin/library/resources?${queryString(query)}`, token)
  }

  setStatus(id: string, action: 'publish' | 'archive', token: string) {
    return this.post<LibraryResourceDetail>(`/admin/library/resources/${encode(id)}/${action}`, {}, token)
  }

  // Personal study data.
  getStorage(token: string) {
    return this.get<LibraryStorageUsage>('/me/library/storage', token)
  }

  getBookmarks(query: { pageNumber?: number; pageSize?: number }, token: string) {
    return this.get<PagedResult<LibraryBookmark>>(`/me/library/bookmarks?${queryString(query)}`, token)
  }

  addBookmark(input: { resourceId: string; chapterId: string | null; page: number | null; label: string | null }, token: string) {
    return this.post<LibraryBookmark>('/me/library/bookmarks', input, token)
  }

  deleteBookmark(id: string, token: string) {
    return this.delete(`/me/library/bookmarks/${encode(id)}`, token)
  }

  getProgressList(query: { status?: LibraryReadingStatus | ''; pageNumber?: number; pageSize?: number }, token: string) {
    return this.get<PagedResult<LibraryProgress>>(`/me/library/progress?${queryString(query)}`, token)
  }

  updateProgress(resourceId: string, input: LibraryProgressInput, token: string) {
    return this.put<LibraryProgress>(`/me/library/resources/${encode(resourceId)}/progress`, input, token)
  }

  clearProgress(resourceId: string, token: string) {
    return this.delete(`/me/library/resources/${encode(resourceId)}/progress`, token)
  }

  createNote(resourceId: string, input: { chapterId: string | null; page: number | null; content: string }, token: string) {
    return this.post<LibraryNote>(`/me/library/resources/${encode(resourceId)}/notes`, input, token)
  }

  updateNote(id: string, input: { chapterId: string | null; page: number | null; content: string }, token: string) {
    return this.put<LibraryNote>(`/me/library/notes/${encode(id)}`, input, token)
  }

  deleteNote(id: string, token: string) {
    return this.delete(`/me/library/notes/${encode(id)}`, token)
  }

  getCollections(token: string) {
    return this.get<LibraryCollection[]>('/me/library/collections', token)
  }

  getCollection(id: string, token: string) {
    return this.get<LibraryCollectionDetail>(`/me/library/collections/${encode(id)}`, token)
  }

  createCollection(input: { name: string; description: string | null }, token: string) {
    return this.post<LibraryCollection>('/me/library/collections', input, token)
  }

  updateCollection(id: string, input: { name: string; description: string | null }, token: string) {
    return this.put<LibraryCollection>(`/me/library/collections/${encode(id)}`, input, token)
  }

  deleteCollection(id: string, token: string) {
    return this.delete(`/me/library/collections/${encode(id)}`, token)
  }

  addCollectionItem(collectionId: string, resourceId: string, token: string) {
    return this.post<void>(`/me/library/collections/${encode(collectionId)}/items`, { resourceId }, token)
  }

  removeCollectionItem(collectionId: string, resourceId: string, token: string) {
    return this.delete(`/me/library/collections/${encode(collectionId)}/items/${encode(resourceId)}`, token)
  }
}

function managePath(mode: LibraryMode) {
  return mode === 'admin' ? '/admin/library' : '/me/library'
}

export const libraryApi = new LibraryApi()
