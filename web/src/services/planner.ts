import { ApiClient, type PagedResult } from './api'

export type StudyStage = 'Preliminary' | 'Written' | 'Viva' | 'General'
export type PlannerPeriod = 'Daily' | 'Weekly' | 'Monthly'
export type GoalMetric = 'StudyMinutes' | 'CompletedTasks' | 'CompletedTopics' | 'McqQuestionsAnswered' | 'WrittenSubmissions' | 'VivaSessions' | 'RevisionReviews'
export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Urgent'
export type TaskStatus = 'Planned' | 'InProgress' | 'Completed' | 'Skipped'
export type StudyActivity = 'Reading' | 'Practice' | 'Writing' | 'Revision' | 'VivaPractice' | 'Other'
export type RevisionSourceType = 'Topic' | 'McqQuestion' | 'WrittenQuestion' | 'VivaQuestion' | 'Mistake' | 'WrittenNote' | 'LibraryNote' | 'VivaNote' | 'CurrentAffairs' | 'Custom'
export type RevisionOutcome = 'Again' | 'Hard' | 'Good' | 'Easy'
export type MistakeCategory = 'Concept' | 'Recall' | 'Calculation' | 'Misreading' | 'Careless' | 'TimePressure' | 'Language' | 'Other'
export type MistakeSourceType = 'Mcq' | 'Written' | 'Viva' | 'Other'
export type ImportSource = 'McqBookmarks' | 'WrittenBookmarks' | 'VivaBookmarks' | 'McqMistakes' | 'MistakeNotebook' | 'WeakTopics' | 'WrittenNotes' | 'LibraryNotes' | 'VivaNotes'

export interface PlannerSettings {
  revisionIntervalsDays: number[]
  againStep: number
  hardStepDelta: number
  goodStepDelta: number
  easyStepDelta: number
  retireAfterStep: number | null
  dailyRevisionLimit: number
  streakMinimumMinutes: number
  weakTopicAccuracyPercent: number
  weakTopicMinimumAnswers: number
  autoQueueMistakes: boolean
  isDefault?: boolean
}

export interface StudyGoal {
  id: string; period: PlannerPeriod; periodStart: string; periodEnd: string; title: string; metric: GoalMetric
  targetValue: number; currentValue: number; percentComplete: number; subjectId: string | null; subjectName: string | null; stage: StudyStage | null; notes: string | null
}
export interface StudyGoalInput { period: PlannerPeriod; date: string; title: string; metric: GoalMetric; targetValue: number; subjectId: string | null; stage: StudyStage | null; notes: string | null }

export interface StudyTask {
  id: string; title: string; description: string | null; priority: TaskPriority; status: TaskStatus; scheduledDate: string; startTime: string | null
  plannedMinutes: number; actualMinutes: number; hasSessions: boolean; subjectId: string | null; subjectName: string | null
  syllabusItemId: string | null; topicTitle: string | null; stage: StudyStage | null; scheduleId: string | null; completedAt: string | null
}
export interface StudyTaskInput {
  title: string; description: string | null; priority: TaskPriority; scheduledDate: string; startTime: string | null; plannedMinutes: number
  subjectId: string | null; syllabusItemId: string | null; stage: StudyStage | null
}

export interface StudySchedule {
  id: string; title: string; days: string[]; startTime: string | null; plannedMinutes: number; priority: TaskPriority
  subjectId: string | null; subjectName: string | null; syllabusItemId: string | null; topicTitle: string | null; stage: StudyStage | null
  startDate: string; endDate: string | null; isActive: boolean
}
export type StudyScheduleInput = Omit<StudySchedule, 'id' | 'subjectName' | 'topicTitle'>

export interface StudySession {
  id: string; startedAt: string; endedAt: string | null; durationMinutes: number; isRunning: boolean; activity: StudyActivity
  taskId: string | null; taskTitle: string | null; subjectId: string | null; subjectName: string | null; syllabusItemId: string | null; topicTitle: string | null; stage: StudyStage | null; notes: string | null
}
export interface StudySessionInput { startedAt: string; durationMinutes: number; activity: StudyActivity; taskId: string | null; subjectId: string | null; syllabusItemId: string | null; stage: StudyStage | null; notes: string | null }

export interface CalendarDay { date: string; tasks: StudyTask[]; studyMinutes: number; revisionDue: number; isActiveDay: boolean }

export interface RevisionItem {
  id: string; sourceType: RevisionSourceType; sourceId: string | null; title: string; syllabusItemId: string | null; topicTitle: string | null
  reason: string; status: 'Active' | 'Suspended' | 'Retired'; step: number; intervalDays: number; dueOn: string; isDue: boolean
  reviewCount: number; lapseCount: number; lastOutcome: RevisionOutcome | null; lastReviewedAt: string | null
}
export interface RevisionQueue { items: RevisionItem[]; dueCount: number; overdueCount: number; shownLimit: number; currentAffairsDue: number; totalCount: number; pageNumber: number; pageSize: number }
export interface RevisionLog { id: string; sourceType: RevisionSourceType; sourceId: string | null; title: string; outcome: RevisionOutcome; stepBefore: number; stepAfter: number; intervalDays: number; reviewedAt: string }
export interface WeakTopic { syllabusItemId: string; title: string; subjectName: string; stageCode: string; answered: number; correct: number; accuracyPercent: number | null; mastery: number | null; openMistakes: number; reasons: string[] }

export interface Mistake {
  id: string; sourceType: MistakeSourceType; questionId: string | null; questionText: string; yourAnswer: string | null; correctAnswerReference: string
  explanation: string | null; category: MistakeCategory; status: 'Open' | 'Resolved'; syllabusItemId: string | null; topicTitle: string | null
  reviewOn: string | null; revisionItemId: string | null; history: RevisionLog[]; createdAt: string; updatedAt: string; resolvedAt: string | null
}
export interface MistakeInput {
  sourceType: MistakeSourceType; questionId: string | null; questionText: string; yourAnswer: string | null; correctAnswerReference: string
  explanation: string | null; category: MistakeCategory; syllabusItemId: string | null; reviewOn: string | null
}

export interface SubjectProgress { subjectId: string; subjectName: string; totalTopics: number; completedTopics: number; percent: number; averageMastery: number | null; studyMinutes: number; mcqAnswered: number; mcqAccuracyPercent: number | null }
export interface StageProgress { stageCode: string; stageName: string; syllabusVersionId: string; versionLabel: string; trackCode: string; totalTopics: number; completedTopics: number; percent: number; subjects: SubjectProgress[] }
export interface Practice { mcqAttempts: number; mcqAnswered: number; mcqCorrect: number; mcqAccuracyPercent: number | null; writtenDrafts: number; writtenSubmissions: number; writtenWords: number; vivaSessionsCompleted: number; vivaAverageRating: number | null }
export interface Streak { current: number; longest: number; minimumMinutes: number }
export interface ProgressOverview { stages: StageProgress[]; totalStudyMinutes: number; totalSessions: number; streak: Streak; practice: Practice }
export interface TopicStatus { syllabusItemId: string; title: string; kind: 'Topic' | 'Subtopic'; parentId: string | null; subjectId: string; subjectName: string; isCompleted: boolean; completedAt: string | null; mastery: number | null; mcqAnswered: number; mcqAccuracyPercent: number | null; studyMinutes: number }
export interface SubjectAccuracy { subjectId: string; subjectName: string; answered: number; correct: number; accuracyPercent: number | null }
export interface NamedMinutes { key: string; label: string; minutes: number }
export interface StudyReport {
  period: PlannerPeriod; label: string; from: string; to: string; studyMinutes: number; sessionCount: number; activeDays: number
  minutesByDay: { date: string; minutes: number }[]; minutesBySubject: NamedMinutes[]; minutesByStage: NamedMinutes[]; minutesByActivity: NamedMinutes[]
  tasks: { planned: number; completed: number; skipped: number; open: number; plannedMinutes: number; actualMinutes: number }
  topicsCompleted: number; completedTopicTitles: string[]; practice: Practice; mcqBySubject: SubjectAccuracy[]; weakSubjects: SubjectAccuracy[]
  revision: { reviews: number; byOutcome: Record<RevisionOutcome, number>; retentionPercent: number | null; activeItems: number; dueNow: number; overdue: number; retiredItems: number }
  goals: StudyGoal[]
}
export interface Dashboard {
  today: string; todayTasks: StudyTask[]; activeSession: StudySession | null; todayMinutes: number; weekMinutes: number; monthMinutes: number
  streak: Streak; revisionDue: number; currentAffairsDue: number; openMistakes: number; goals: StudyGoal[]; stages: StageProgress[]
  last30Days: Practice; weakSubjects: SubjectAccuracy[]; recentActivity: { type: string; title: string; occurredAt: string; detail: string | null }[]
}

function queryString(params: object) {
  const search = new URLSearchParams()
  Object.entries(params as Record<string, string | number | boolean | undefined | null>).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value))
  })
  return search.toString()
}

const encode = encodeURIComponent

class PlannerApi extends ApiClient {
  getSettings(token: string) { return this.get<PlannerSettings>('/me/planner/settings', token) }
  saveSettings(input: PlannerSettings, token: string) { return this.put<PlannerSettings>('/me/planner/settings', input, token) }

  getGoals(query: { period?: PlannerPeriod; date?: string }, token: string) { return this.get<StudyGoal[]>(`/me/planner/goals?${queryString(query)}`, token) }
  createGoal(input: StudyGoalInput, token: string) { return this.post<StudyGoal>('/me/planner/goals', input, token) }
  updateGoal(id: string, input: StudyGoalInput, token: string) { return this.put<StudyGoal>(`/me/planner/goals/${encode(id)}`, input, token) }
  deleteGoal(id: string, token: string) { return this.delete(`/me/planner/goals/${encode(id)}`, token) }

  getTasks(query: { from?: string; to?: string; status?: string; pageSize?: number }, token: string) { return this.get<PagedResult<StudyTask>>(`/me/planner/tasks?${queryString(query)}`, token) }
  createTask(input: StudyTaskInput, token: string) { return this.post<StudyTask>('/me/planner/tasks', input, token) }
  updateTask(id: string, input: StudyTaskInput, token: string) { return this.put<StudyTask>(`/me/planner/tasks/${encode(id)}`, input, token) }
  setTaskStatus(id: string, status: TaskStatus, actualMinutes: number | null, token: string) { return this.post<StudyTask>(`/me/planner/tasks/${encode(id)}/status`, { status, actualMinutes }, token) }
  deleteTask(id: string, token: string) { return this.delete(`/me/planner/tasks/${encode(id)}`, token) }

  getSchedules(token: string) { return this.get<StudySchedule[]>('/me/planner/schedules', token) }
  createSchedule(input: StudyScheduleInput, token: string) { return this.post<StudySchedule>('/me/planner/schedules', input, token) }
  updateSchedule(id: string, input: StudyScheduleInput, token: string) { return this.put<StudySchedule>(`/me/planner/schedules/${encode(id)}`, input, token) }
  deleteSchedule(id: string, token: string) { return this.delete(`/me/planner/schedules/${encode(id)}`, token) }
  generateTasks(id: string, from: string, to: string, token: string) { return this.post<{ created: number }>(`/me/planner/schedules/${encode(id)}/generate`, { from, to }, token) }

  getSessions(query: { from?: string; to?: string; pageSize?: number }, token: string) { return this.get<PagedResult<StudySession>>(`/me/planner/sessions?${queryString(query)}`, token) }
  getActiveSession(token: string) { return this.get<StudySession | undefined>('/me/planner/sessions/active', token) }
  startSession(input: { activity: StudyActivity; taskId: string | null; subjectId: string | null; syllabusItemId: string | null; stage: StudyStage | null }, token: string) {
    return this.post<StudySession>('/me/planner/sessions/start', input, token)
  }
  stopSession(id: string, notes: string | null, token: string) { return this.post<StudySession>(`/me/planner/sessions/${encode(id)}/stop`, { notes }, token) }
  logSession(input: StudySessionInput, token: string) { return this.post<StudySession>('/me/planner/sessions', input, token) }
  deleteSession(id: string, token: string) { return this.delete(`/me/planner/sessions/${encode(id)}`, token) }

  getCalendar(from: string, to: string, token: string) { return this.get<{ from: string; to: string; days: CalendarDay[] }>(`/me/planner/calendar?${queryString({ from, to })}`, token) }

  getQueue(query: { dueOnly?: boolean; sourceType?: string; status?: string; pageNumber?: number }, token: string) { return this.get<RevisionQueue>(`/me/revision/queue?${queryString(query)}`, token) }
  addRevisionItem(input: { sourceType: RevisionSourceType; sourceId: string | null; title: string | null; dueOn: string | null }, token: string) { return this.post<RevisionItem>('/me/revision/items', input, token) }
  review(id: string, outcome: RevisionOutcome, token: string) { return this.post<RevisionItem>(`/me/revision/items/${encode(id)}/review`, { outcome }, token) }
  setRevisionStatus(id: string, action: 'suspend' | 'resume', token: string) { return this.post<RevisionItem>(`/me/revision/items/${encode(id)}/${action}`, {}, token) }
  deleteRevisionItem(id: string, token: string) { return this.delete(`/me/revision/items/${encode(id)}`, token) }
  importRevision(source: ImportSource, token: string) { return this.post<{ source: string; added: number; alreadyQueued: number }>('/me/revision/import', { source }, token) }
  getHistory(pageNumber: number, token: string) { return this.get<PagedResult<RevisionLog>>(`/me/revision/history?pageNumber=${pageNumber}&pageSize=50`, token) }
  getWeakTopics(token: string) { return this.get<WeakTopic[]>('/me/revision/weak-topics', token) }

  getMistakes(query: { search?: string; status?: string; category?: string; sourceType?: string; pageNumber?: number }, token: string) {
    return this.get<PagedResult<Mistake>>(`/me/mistakes?${queryString({ ...query, pageSize: 20 })}`, token)
  }
  getMistake(id: string, token: string) { return this.get<Mistake>(`/me/mistakes/${encode(id)}`, token) }
  createMistake(input: MistakeInput, token: string) { return this.post<Mistake>('/me/mistakes', input, token) }
  updateMistake(id: string, input: MistakeInput, token: string) { return this.put<Mistake>(`/me/mistakes/${encode(id)}`, input, token) }
  setMistakeStatus(id: string, action: 'resolve' | 'reopen', token: string) { return this.post<Mistake>(`/me/mistakes/${encode(id)}/${action}`, {}, token) }
  deleteMistake(id: string, token: string) { return this.delete(`/me/mistakes/${encode(id)}`, token) }
  importMcqMistakes(token: string) { return this.post<{ created: number }>('/me/mistakes/import-mcq', {}, token) }

  getDashboard(token: string) { return this.get<Dashboard>('/me/dashboard', token) }
  getProgress(token: string) { return this.get<ProgressOverview>('/me/progress', token) }
  getTopics(versionId: string, query: { subjectId?: string; status?: string }, token: string) { return this.get<TopicStatus[]>(`/me/progress/topics?${queryString({ versionId, ...query })}`, token) }
  setMastery(itemId: string, mastery: number | null, token: string) { return this.put<TopicStatus>(`/me/progress/topics/${encode(itemId)}/mastery`, { mastery }, token) }
  getReport(period: 'daily' | 'weekly' | 'monthly', date: string | undefined, token: string) { return this.get<StudyReport>(`/me/reports?${queryString({ period, date })}`, token) }
}

export const plannerApi = new PlannerApi()
