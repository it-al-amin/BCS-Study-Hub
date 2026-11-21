import { ApiClient, type PagedResult } from './api'

export type McqQuestionType = 'SingleAnswer' | 'MultipleAnswer'
export type McqDifficulty = 'Easy' | 'Medium' | 'Hard'
export type McqContentStatus = 'Draft' | 'Published' | 'Archived'
export type McqPracticeMode = 'Topic' | 'Subject' | 'Mixed' | 'MockExam'
export type McqAttemptStatus = 'InProgress' | 'Submitted'
export type McqMistakeStatus = 'Open' | 'Reviewed' | 'Resolved'

export interface McqQuestionOption {
  id: string
  text: string
  sortOrder: number
  isCorrect?: boolean | null
}

export interface McqQuestionSource {
  id?: string
  title: string
  publisher: string
  url: string
  citation: string | null
  publishedOn: string | null
  verificationStatus: 'Unverified' | 'Verified' | 'NeedsReview'
}

export interface McqQuestion {
  id: string
  syllabusItemId: string
  subjectId: string
  subjectName: string
  topicTitle: string
  parentTopicTitle: string | null
  prompt: string
  imageUrl: string | null
  type: McqQuestionType
  difficulty: McqDifficulty
  status: McqContentStatus
  bcsExamYear: number | null
  examNumber: number | null
  examReference: string | null
  isBookmarked: boolean
  options: McqQuestionOption[]
  sources: McqQuestionSource[]
  tags: string[]
}

export interface McqAdminQuestion extends Omit<McqQuestion, 'subjectId' | 'subjectName' | 'topicTitle' | 'parentTopicTitle' | 'isBookmarked'> {
  explanation: string | null
  syllabusVersionId: string
}

export interface McqOptionInput {
  text: string
  isCorrect: boolean
}

export interface McqQuestionInput {
  syllabusItemId: string
  prompt: string
  explanation: string | null
  imageUrl: string | null
  type: McqQuestionType
  difficulty: McqDifficulty
  bcsExamYear: number | null
  examNumber: number | null
  examReference: string | null
  options: McqOptionInput[]
  sources: Omit<McqQuestionSource, 'id'>[]
  tags: string[]
}

export interface McqQuestionQuery {
  search?: string
  syllabusVersionId?: string
  subjectId?: string
  syllabusItemId?: string
  bcsExamYear?: number
  difficulty?: McqDifficulty
  tag?: string
  bookmarkedOnly?: boolean
  pageNumber?: number
  pageSize?: number
  sortBy?: 'recent' | 'year' | 'difficulty'
}

export interface McqExamSet {
  id: string
  syllabusVersionId: string
  syllabusVersionLabel: string
  subjectId: string | null
  subjectName: string | null
  syllabusItemId: string | null
  syllabusItemTitle: string | null
  name: string
  instructions: string
  mode: McqPracticeMode
  status: McqContentStatus
  questionCount: number
  durationSeconds: number | null
  bcsExamYear: number | null
  difficulty: McqDifficulty | null
  marksCorrect: number
  marksIncorrect: number
  marksUnanswered: number
  showFeedbackImmediately: boolean
}

export interface McqExamSetInput {
  syllabusVersionId: string
  subjectId: string | null
  syllabusItemId: string | null
  name: string
  instructions: string
  mode: McqPracticeMode
  questionCount: number
  durationSeconds: number | null
  bcsExamYear: number | null
  difficulty: McqDifficulty | null
  marksCorrect: number
  marksIncorrect: number
  marksUnanswered: number
  showFeedbackImmediately: boolean
}

export interface McqAttemptQuestion {
  id: string
  questionId: string
  position: number
  prompt: string
  imageUrl: string | null
  type: McqQuestionType
  difficulty: McqDifficulty
  options: McqQuestionOption[]
  selectedOptionIds: string[]
  isMarkedForReview: boolean
}

export interface McqAttempt {
  id: string
  examSetId: string
  examSetName: string
  mode: McqPracticeMode
  status: McqAttemptStatus
  startedAt: string
  expiresAt: string | null
  submittedAt: string | null
  isTimedOut: boolean
  score: number | null
  maximumScore: number
  correctCount: number
  incorrectCount: number
  unansweredCount: number
  questions: McqAttemptQuestion[]
}

export interface McqAttemptReviewQuestion extends McqAttemptQuestion {
  explanation: string | null
  isCorrect: boolean
  marksAwarded: number
  syllabusItemId: string
  topicTitle: string
  parentTopicTitle: string | null
  sources: McqQuestionSource[]
}

export interface McqAttemptReview {
  attempt: McqAttempt
  questions: McqAttemptReviewQuestion[]
}

export interface McqAttemptListItem {
  id: string
  examSetId: string
  examSetName: string
  mode: McqPracticeMode
  status: McqAttemptStatus
  startedAt: string
  submittedAt: string | null
  isTimedOut: boolean
  score: number | null
  maximumScore: number
  correctCount: number
  incorrectCount: number
  unansweredCount: number
}

export interface McqMistake {
  questionId: string
  prompt: string
  difficulty: McqDifficulty
  syllabusItemId: string
  topicTitle: string
  parentTopicTitle: string | null
  status: McqMistakeStatus
  notes: string | null
  lastAttemptId: string | null
  updatedAt: string
}

export interface McqAnalytics {
  attemptCount: number
  answerCount: number
  correctCount: number
  incorrectCount: number
  unansweredCount: number
  accuracyPercent: number
  averageScore: number | null
  topicPerformance: { syllabusItemId: string; topicTitle: string; answerCount: number; correctCount: number; accuracyPercent: number }[]
  recentAttempts: McqAttemptListItem[]
}

export interface McqPageQuery {
  pageNumber?: number
  pageSize?: number
}

function queryString(params: object) {
  const search = new URLSearchParams()
  Object.entries(params as Record<string, string | number | boolean | undefined | null>).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value))
  })
  return search.toString()
}

class McqApi extends ApiClient {
  getQuestions(query: McqQuestionQuery, token?: string) {
    return this.get<PagedResult<McqQuestion>>(`/questions?${queryString(query)}`, token)
  }

  getQuestion(id: string, token?: string) {
    return this.get<McqQuestion>(`/questions/${id}`, token)
  }

  getAdminQuestions(query: McqPageQuery & { search?: string; status?: string }, token: string) {
    return this.get<PagedResult<McqAdminQuestion>>(`/admin/mcq/questions?${queryString(query)}`, token)
  }

  createQuestion(input: McqQuestionInput, token: string) {
    return this.post<McqAdminQuestion>('/admin/mcq/questions', input, token)
  }

  updateQuestion(id: string, input: McqQuestionInput, token: string) {
    return this.put<McqAdminQuestion>(`/admin/mcq/questions/${id}`, input, token)
  }

  setQuestionStatus(id: string, status: 'publish' | 'archive', token: string) {
    return this.post<McqAdminQuestion>(`/admin/mcq/questions/${id}/${status}`, {}, token)
  }

  getExamSets(query: McqPageQuery = {}, token?: string) {
    return this.get<PagedResult<McqExamSet>>(`/practice/mcq/sets?${queryString(query)}`, token)
  }

  getAdminExamSets(query: McqPageQuery, token: string) {
    return this.get<PagedResult<McqExamSet>>(`/admin/mcq/sets?${queryString(query)}`, token)
  }

  createExamSet(input: McqExamSetInput, token: string) {
    return this.post<McqExamSet>('/admin/mcq/sets', input, token)
  }

  updateExamSet(id: string, input: McqExamSetInput, token: string) {
    return this.put<McqExamSet>(`/admin/mcq/sets/${id}`, input, token)
  }

  setExamSetStatus(id: string, status: 'publish' | 'archive', token: string) {
    return this.post<McqExamSet>(`/admin/mcq/sets/${id}/${status}`, {}, token)
  }

  createAttempt(examSetId: string, token: string) {
    return this.post<McqAttempt>('/practice/mcq/attempts', { examSetId }, token)
  }

  getAttemptHistory(query: McqPageQuery, token: string) {
    return this.get<PagedResult<McqAttemptListItem>>(`/practice/mcq/attempts?${queryString(query)}`, token)
  }

  getAttempt(id: string, token: string) {
    return this.get<McqAttempt>(`/practice/mcq/attempts/${id}`, token)
  }

  updateAnswer(attemptId: string, questionId: string, selectedOptionIds: string[], isMarkedForReview: boolean, token: string) {
    return this.put<{ questionId: string; selectedOptionIds: string[]; isMarkedForReview: boolean; isCorrect: boolean | null; explanation: string | null }>(
      `/practice/mcq/attempts/${attemptId}/questions/${questionId}/answer`,
      { selectedOptionIds, isMarkedForReview },
      token,
    )
  }

  submitAttempt(attemptId: string, token: string) {
    return this.post<McqAttempt>(`/practice/mcq/attempts/${attemptId}/submit`, {}, token)
  }

  getReview(attemptId: string, token: string) {
    return this.get<McqAttemptReview>(`/practice/mcq/attempts/${attemptId}/review`, token)
  }

  getBookmarks(query: McqPageQuery, token: string) {
    return this.get<PagedResult<McqQuestion>>(`/me/questions/bookmarks?${queryString(query)}`, token)
  }

  addBookmark(id: string, token: string) {
    return this.post<void>(`/me/questions/${id}/bookmark`, {}, token)
  }

  removeBookmark(id: string, token: string) {
    return this.delete(`/me/questions/${id}/bookmark`, token)
  }

  getMistakes(query: McqPageQuery & { status?: string }, token: string) {
    return this.get<PagedResult<McqMistake>>(`/me/questions/mistakes?${queryString(query)}`, token)
  }

  updateMistake(id: string, status: McqMistakeStatus, notes: string | null, token: string) {
    return this.put<McqMistake>(`/me/questions/${id}/mistake`, { status, notes }, token)
  }

  getAnalytics(token: string) {
    return this.get<McqAnalytics>('/me/mcq/analytics', token)
  }
}

export const mcqApi = new McqApi()
