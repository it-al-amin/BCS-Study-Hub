import { ApiClient, type PagedResult } from './api'

export type VivaQuestionStatus = 'Draft' | 'Published' | 'Archived'
export type VivaReferenceType = 'QuestionSource' | 'SuggestedAnswer'
export type VivaReferenceVerificationStatus = 'Unverified' | 'Verified' | 'NeedsReview'

export interface VivaCategory {
  id: string
  slug: string
  name: string
  description: string | null
  displayOrder: number
  isActive: boolean
  questionCount: number
}

export interface VivaCategoryInput {
  slug: string
  name: string
  description: string | null
  displayOrder: number
  isActive: boolean
}

export interface VivaQuestionReference {
  id: string
  type: VivaReferenceType
  title: string
  publisher: string
  url: string
  citation: string | null
  verificationStatus: VivaReferenceVerificationStatus
}

export type VivaQuestionReferenceInput = Omit<VivaQuestionReference, 'id'>

export interface VivaQuestion {
  id: string
  categoryId: string
  categoryName: string
  topic: string | null
  prompt: string
  bcsExamYear: number | null
  examNumber: number | null
  examReference: string | null
  isBookmarked: boolean
  references: VivaQuestionReference[]
}

export interface VivaAdminQuestion {
  question: VivaQuestion
  status: VivaQuestionStatus
}

export interface VivaQuestionInput {
  categoryId: string
  topic: string | null
  prompt: string
  bcsExamYear: number | null
  examNumber: number | null
  examReference: string | null
  references: VivaQuestionReferenceInput[]
}

export interface VivaAnswerNoteRevision {
  revisionNumber: number
  content: string
  createdAt: string
}

export interface VivaAnswerNote {
  questionId: string
  content: string
  createdAt: string
  updatedAt: string
  revisions: VivaAnswerNoteRevision[]
}

export interface VivaQuestionHistory {
  questionId: string
  prompt: string
  categoryName: string
  topic: string | null
  lastViewedAt: string
  viewCount: number
}

export interface VivaMockSessionQuestion {
  id: string
  questionId: string
  sequence: number
  prompt: string
  categoryName: string
  topic: string | null
  answerNotes: string
  needsRevision: boolean
}

export interface VivaMockSession {
  id: string
  title: string
  status: 'InProgress' | 'Completed'
  startedAt: string
  completedAt: string | null
  communicationRating: number | null
  subjectKnowledgeRating: number | null
  clarityRating: number | null
  confidenceRating: number | null
  feedbackNotes: string | null
  improvementAreas: string | null
  questions: VivaMockSessionQuestion[]
}

export interface VivaMockSessionListItem {
  id: string
  title: string
  status: 'InProgress' | 'Completed'
  questionCount: number
  startedAt: string
  completedAt: string | null
  overallRating: number | null
}

export interface VivaWeakTopic {
  categoryName: string
  topic: string | null
  revisionCount: number
}

export interface VivaProfile {
  selfIntroduction: string
  academicBackground: string
  universityAndSubject: string
  homeDistrict: string
  professionalExperience: string
  cadrePreferences: string
  interestsAndHobbies: string
  strengthsAndDevelopmentAreas: string
  updatedAt: string | null
}

export interface VivaProfileInput extends Omit<VivaProfile, 'updatedAt'> {}

function queryString(params: object) {
  const search = new URLSearchParams()
  Object.entries(params as Record<string, string | number | boolean | undefined | null>).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value))
  })
  return search.toString()
}

class VivaApi extends ApiClient {
  getCategories() {
    return this.get<VivaCategory[]>('/viva/categories')
  }

  getTopics(categoryId?: string) {
    return this.get<string[]>(`/viva/topics?${queryString({ categoryId })}`)
  }

  getQuestions(query: {
    search?: string
    categoryId?: string
    topic?: string
    bookmarkedOnly?: boolean
    pageNumber?: number
    pageSize?: number
    sortBy?: 'recent' | 'category' | 'topic'
  } = {}, token?: string) {
    return this.get<PagedResult<VivaQuestion>>(`/viva/questions?${queryString(query)}`, token)
  }

  getQuestion(id: string, token?: string) {
    return this.get<VivaQuestion>(`/viva/questions/${id}`, token)
  }

  getAdminCategories(token: string) {
    return this.get<VivaCategory[]>('/admin/viva/categories', token)
  }

  createCategory(input: VivaCategoryInput, token: string) {
    return this.post<VivaCategory>('/admin/viva/categories', input, token)
  }

  updateCategory(id: string, input: VivaCategoryInput, token: string) {
    return this.put<VivaCategory>(`/admin/viva/categories/${id}`, input, token)
  }

  getAdminQuestions(query: { search?: string; status?: VivaQuestionStatus; pageNumber?: number; pageSize?: number }, token: string) {
    return this.get<PagedResult<VivaAdminQuestion>>(`/admin/viva/questions?${queryString(query)}`, token)
  }

  getAdminQuestion(id: string, token: string) {
    return this.get<VivaAdminQuestion>(`/admin/viva/questions/${id}`, token)
  }

  createQuestion(input: VivaQuestionInput, token: string) {
    return this.post<VivaAdminQuestion>('/admin/viva/questions', input, token)
  }

  updateQuestion(id: string, input: VivaQuestionInput, token: string) {
    return this.put<VivaAdminQuestion>(`/admin/viva/questions/${id}`, input, token)
  }

  setQuestionStatus(id: string, status: 'publish' | 'archive', token: string) {
    return this.post<VivaAdminQuestion>(`/admin/viva/questions/${id}/${status}`, {}, token)
  }

  addBookmark(questionId: string, token: string) {
    return this.post<void>(`/me/viva/questions/${questionId}/bookmark`, {}, token)
  }

  removeBookmark(questionId: string, token: string) {
    return this.delete(`/me/viva/questions/${questionId}/bookmark`, token)
  }

  getAnswerNote(questionId: string, token: string) {
    return this.get<VivaAnswerNote>(`/me/viva/questions/${questionId}/answer-note`, token)
  }

  saveAnswerNote(questionId: string, content: string, token: string) {
    return this.put<VivaAnswerNote>(`/me/viva/questions/${questionId}/answer-note`, { content }, token)
  }

  getQuestionHistory(query: { pageNumber?: number; pageSize?: number }, token: string) {
    return this.get<PagedResult<VivaQuestionHistory>>(`/me/viva/question-history?${queryString(query)}`, token)
  }

  getSessions(query: { pageNumber?: number; pageSize?: number }, token: string) {
    return this.get<PagedResult<VivaMockSessionListItem>>(`/me/viva/sessions?${queryString(query)}`, token)
  }

  createSession(title: string, questionIds: string[], token: string) {
    return this.post<VivaMockSession>('/me/viva/sessions', { title, questionIds }, token)
  }

  getSession(id: string, token: string) {
    return this.get<VivaMockSession>(`/me/viva/sessions/${id}`, token)
  }

  updateSessionQuestion(
    sessionId: string,
    sessionQuestionId: string,
    answerNotes: string,
    needsRevision: boolean,
    token: string,
  ) {
    return this.put<VivaMockSession>(`/me/viva/sessions/${sessionId}/questions/${sessionQuestionId}`, { answerNotes, needsRevision }, token)
  }

  completeSession(
    id: string,
    evaluation: {
      communicationRating: number | null
      subjectKnowledgeRating: number | null
      clarityRating: number | null
      confidenceRating: number | null
      feedbackNotes: string
      improvementAreas: string
    },
    token: string,
  ) {
    return this.post<VivaMockSession>(`/me/viva/sessions/${id}/complete`, evaluation, token)
  }

  getWeakTopics(token: string) {
    return this.get<VivaWeakTopic[]>('/me/viva/weak-topics', token)
  }

  getProfile(token: string) {
    return this.get<VivaProfile>('/me/viva/profile', token)
  }

  saveProfile(input: VivaProfileInput, token: string) {
    return this.put<VivaProfile>('/me/viva/profile', input, token)
  }
}

export const vivaApi = new VivaApi()
