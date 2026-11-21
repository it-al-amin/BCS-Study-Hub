import { ApiClient, type PagedResult } from './api'

export type WrittenQuestionType = 'Descriptive' | 'Analytical' | 'Essay' | 'Composition' | 'Translation'
export type WrittenQuestionStatus = 'Draft' | 'Published' | 'Archived'
export type WrittenAnswerStatus = 'Draft' | 'Submitted'
export type WrittenReferenceStatus = 'Unverified' | 'Verified' | 'NeedsReview'

export interface WrittenSource {
  id?: string
  title: string
  publisher: string
  url: string
  citation: string | null
  publishedOn?: string | null
  verificationStatus: WrittenReferenceStatus
}

export interface WrittenResource {
  id?: string
  title: string
  url: string
}

export interface WrittenQuestion {
  id: string
  syllabusItemId: string
  subjectId: string
  subjectName: string
  syllabusVersionId: string
  syllabusVersionLabel: string
  topicTitle: string
  parentTopicTitle: string | null
  paperId: string | null
  paperName: string | null
  prompt: string
  type: WrittenQuestionType
  bcsExamYear: number | null
  examNumber: number | null
  examReference: string | null
  marks: number | null
  answerGuidance: string | null
  status: WrittenQuestionStatus
  isBookmarked: boolean
  hasModelAnswer: boolean
  sources: WrittenSource[]
  resources: WrittenResource[]
  tags: string[]
}

export interface WrittenAdminQuestion {
  question: WrittenQuestion
  modelAnswerContent: string | null
  modelAnswerStatus: 'Draft' | 'Published' | 'Archived' | null
  modelAnswerReferences: WrittenSource[]
}

export interface WrittenQuestionInput {
  syllabusItemId: string
  prompt: string
  type: WrittenQuestionType
  bcsExamYear: number | null
  examNumber: number | null
  examReference: string | null
  marks: number | null
  answerGuidance: string | null
  sources: Omit<WrittenSource, 'id'>[]
  resources: Omit<WrittenResource, 'id'>[]
  tags: string[]
  modelAnswerContent: string | null
  modelAnswerReferences: Omit<WrittenSource, 'id' | 'publishedOn'>[]
}

export interface WrittenModelAnswer {
  questionId: string
  content: string
  references: WrittenSource[]
}

export interface WrittenSelfAssessment {
  contentCoverage: number | null
  organization: number | null
  clarity: number | null
  languageAccuracy: number | null
  reflection: string | null
}

export interface WrittenAnswer {
  id: string
  questionId: string
  questionPrompt: string
  questionType: WrittenQuestionType
  syllabusItemId: string
  topicTitle: string
  status: WrittenAnswerStatus
  content: string
  wordCount: number
  timeSpentSeconds: number
  selfAssessment: WrittenSelfAssessment | null
  startedAt: string
  updatedAt: string
  submittedAt: string | null
}

export interface WrittenAnswerListItem {
  id: string
  questionId: string
  questionPrompt: string
  questionType: WrittenQuestionType
  syllabusItemId: string
  topicTitle: string
  status: WrittenAnswerStatus
  wordCount: number
  startedAt: string
  updatedAt: string
  submittedAt: string | null
}

export interface WrittenAnswerRevision {
  revisionNumber: number
  kind: 'DraftSave' | 'Submission' | 'RevisionStarted'
  content: string
  wordCount: number
  timeSpentSeconds: number
  selfAssessment: WrittenSelfAssessment | null
  createdAt: string
}

export interface WrittenGoal {
  id: string
  startDate: string
  endDate: string
  targetSubmissions: number
  targetWords: number
  completedSubmissions: number
  completedWords: number
}

export interface WrittenGoalInput {
  startDate: string
  endDate: string
  targetSubmissions: number
  targetWords: number
}

export interface WrittenProgress {
  from: string
  to: string
  draftCount: number
  submittedCount: number
  submittedWords: number
  writingTimeSeconds: number
  topics: Array<{
    syllabusItemId: string
    topicTitle: string
    submittedAnswers: number
    averageSelfAssessment: number | null
  }>
}

export interface WrittenStudyNote {
  id: string
  syllabusItemId: string
  syllabusVersionId: string
  topicTitle: string
  title: string
  content: string
  createdAt: string
  updatedAt: string
}

export interface WrittenStudyNoteInput {
  syllabusItemId: string
  title: string
  content: string
}

function queryString(params: object) {
  const search = new URLSearchParams()
  Object.entries(params as Record<string, string | number | boolean | undefined | null>).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value))
  })
  return search.toString()
}

class WrittenApi extends ApiClient {
  getQuestions(query: {
    search?: string
    syllabusVersionId?: string
    subjectId?: string
    syllabusItemId?: string
    type?: WrittenQuestionType
    bcsExamYear?: number
    tag?: string
    pageNumber?: number
    pageSize?: number
    sortBy?: 'recent' | 'year' | 'subject'
  } = {}, token?: string) {
    return this.get<PagedResult<WrittenQuestion>>(`/written/questions?${queryString(query)}`, token)
  }

  getQuestion(id: string, token?: string) {
    return this.get<WrittenQuestion>(`/written/questions/${id}`, token)
  }

  getModelAnswer(questionId: string) {
    return this.get<WrittenModelAnswer>(`/written/questions/${questionId}/model-answer`)
  }

  getAdminQuestions(query: { search?: string; status?: WrittenQuestionStatus; pageNumber?: number; pageSize?: number }, token: string) {
    return this.get<PagedResult<WrittenAdminQuestion>>(`/admin/written/questions?${queryString(query)}`, token)
  }

  getAdminQuestion(id: string, token: string) {
    return this.get<WrittenAdminQuestion>(`/admin/written/questions/${id}`, token)
  }

  createQuestion(input: WrittenQuestionInput, token: string) {
    return this.post<WrittenAdminQuestion>('/admin/written/questions', input, token)
  }

  updateQuestion(id: string, input: WrittenQuestionInput, token: string) {
    return this.put<WrittenAdminQuestion>(`/admin/written/questions/${id}`, input, token)
  }

  setQuestionStatus(id: string, status: 'publish' | 'archive', token: string) {
    return this.post<WrittenAdminQuestion>(`/admin/written/questions/${id}/${status}`, {}, token)
  }

  setModelAnswerStatus(id: string, status: 'publish' | 'archive', token: string) {
    return this.post<WrittenAdminQuestion>(`/admin/written/questions/${id}/model-answer/${status}`, {}, token)
  }

  createAnswer(questionId: string, token: string) {
    return this.post<WrittenAnswer>(`/me/written/questions/${questionId}/answers`, {}, token)
  }

  getAnswer(id: string, token: string) {
    return this.get<WrittenAnswer>(`/me/written/answers/${id}`, token)
  }

  getAnswers(query: { pageNumber?: number; pageSize?: number }, token: string) {
    return this.get<PagedResult<WrittenAnswerListItem>>(`/me/written/answers?${queryString(query)}`, token)
  }

  updateAnswer(id: string, content: string, timeSpentSeconds: number, selfAssessment: WrittenSelfAssessment | null, token: string) {
    return this.put<WrittenAnswer>(`/me/written/answers/${id}`, { content, timeSpentSeconds, selfAssessment }, token)
  }

  submitAnswer(id: string, content: string, timeSpentSeconds: number, selfAssessment: WrittenSelfAssessment | null, token: string) {
    return this.post<WrittenAnswer>(`/me/written/answers/${id}/submit`, { content, timeSpentSeconds, selfAssessment }, token)
  }

  reviseAnswer(id: string, token: string) {
    return this.post<WrittenAnswer>(`/me/written/answers/${id}/revise`, {}, token)
  }

  getRevisions(id: string, token: string) {
    return this.get<WrittenAnswerRevision[]>(`/me/written/answers/${id}/revisions`, token)
  }

  getBookmarks(query: { pageNumber?: number; pageSize?: number }, token: string) {
    return this.get<PagedResult<{ questionId: string; prompt: string; type: WrittenQuestionType; syllabusItemId: string; topicTitle: string; createdAt: string }>>(
      `/me/written/bookmarks?${queryString(query)}`,
      token,
    )
  }

  addBookmark(questionId: string, token: string) {
    return this.post<void>(`/me/written/questions/${questionId}/bookmark`, {}, token)
  }

  removeBookmark(questionId: string, token: string) {
    return this.delete(`/me/written/questions/${questionId}/bookmark`, token)
  }

  getGoals(token: string) {
    return this.get<WrittenGoal[]>('/me/written/goals', token)
  }

  createGoal(input: WrittenGoalInput, token: string) {
    return this.post<WrittenGoal>('/me/written/goals', input, token)
  }

  updateGoal(id: string, input: WrittenGoalInput, token: string) {
    return this.put<WrittenGoal>(`/me/written/goals/${id}`, input, token)
  }

  getProgress(query: { from?: string; to?: string }, token: string) {
    return this.get<WrittenProgress>(`/me/written/progress?${queryString(query)}`, token)
  }

  getStudyNotes(query: { syllabusItemId?: string; pageNumber?: number; pageSize?: number }, token: string) {
    return this.get<PagedResult<WrittenStudyNote>>(`/me/written/notes?${queryString(query)}`, token)
  }

  createStudyNote(input: WrittenStudyNoteInput, token: string) {
    return this.post<WrittenStudyNote>('/me/written/notes', input, token)
  }

  updateStudyNote(id: string, input: WrittenStudyNoteInput, token: string) {
    return this.put<WrittenStudyNote>(`/me/written/notes/${id}`, input, token)
  }

  deleteStudyNote(id: string, token: string) {
    return this.delete(`/me/written/notes/${id}`, token)
  }
}

export const writtenApi = new WrittenApi()
