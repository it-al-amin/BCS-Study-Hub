import type { GoalMetric, MistakeCategory, RevisionSourceType, StudyActivity, TaskPriority, TaskStatus } from '../../services/planner'

/** Calendar dates use Bangladesh Standard Time (UTC+6), matching the server. */
export function todayIso() {
  return new Date(Date.now() + 6 * 60 * 60 * 1000).toISOString().slice(0, 10)
}

export function addDays(iso: string, days: number) {
  const date = new Date(`${iso}T00:00:00Z`)
  date.setUTCDate(date.getUTCDate() + days)
  return date.toISOString().slice(0, 10)
}

export function weekdayOf(iso: string) {
  return new Date(`${iso}T00:00:00Z`).getUTCDay()
}

/** Monday-first 6×7 grid covering the month that contains <paramref name="iso"/>. */
export function monthGrid(iso: string) {
  const first = `${iso.slice(0, 7)}-01`
  const offset = (weekdayOf(first) + 6) % 7
  const start = addDays(first, -offset)
  return Array.from({ length: 42 }, (_, index) => addDays(start, index))
}

export function formatDay(iso: string, options: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'short', year: 'numeric' }) {
  return new Date(`${iso.slice(0, 10)}T00:00:00Z`).toLocaleDateString(undefined, { ...options, timeZone: 'UTC' })
}

export function formatMinutes(minutes: number) {
  if (minutes < 60) return `${minutes} min`
  const hours = Math.floor(minutes / 60)
  const rest = minutes % 60
  return rest === 0 ? `${hours} h` : `${hours} h ${rest} min`
}

export function errorMessage(reason: unknown, fallback: string) {
  return reason instanceof Error ? reason.message : fallback
}

export const priorityTone: Record<TaskPriority, 'neutral' | 'info' | 'warning' | 'danger'> = { Low: 'neutral', Medium: 'info', High: 'warning', Urgent: 'danger' }
export const statusLabels: Record<TaskStatus, string> = { Planned: 'Planned', InProgress: 'In progress', Completed: 'Completed', Skipped: 'Skipped' }
export const activityLabels: Record<StudyActivity, string> = { Reading: 'Reading', Practice: 'Practice', Writing: 'Writing', Revision: 'Revision', VivaPractice: 'Viva practice', Other: 'Other' }
export const metricLabels: Record<GoalMetric, string> = {
  StudyMinutes: 'Study minutes',
  CompletedTasks: 'Completed tasks',
  CompletedTopics: 'Completed topics',
  McqQuestionsAnswered: 'MCQ questions answered',
  WrittenSubmissions: 'Written submissions',
  VivaSessions: 'Viva mock sessions',
  RevisionReviews: 'Revision reviews',
}
export const sourceLabels: Record<RevisionSourceType, string> = {
  Topic: 'Topic', McqQuestion: 'MCQ', WrittenQuestion: 'Written question', VivaQuestion: 'Viva question', Mistake: 'Mistake',
  WrittenNote: 'Written note', LibraryNote: 'Library note', VivaNote: 'Viva note', CurrentAffairs: 'Current affairs', Custom: 'Custom',
}
export const mistakeCategoryLabels: Record<MistakeCategory, string> = {
  Concept: 'Concept gap', Recall: 'Forgot a fact', Calculation: 'Calculation', Misreading: 'Misread the question',
  Careless: 'Careless slip', TimePressure: 'Time pressure', Language: 'Language', Other: 'Other',
}

/** Where a revision item's source can be opened, if it has a page. */
export function sourceLink(type: RevisionSourceType, sourceId: string | null, topicId: string | null) {
  if (type === 'WrittenQuestion' && sourceId) return `/written/questions/${sourceId}`
  if (type === 'VivaQuestion' && sourceId) return `/viva/questions/${sourceId}`
  if (type === 'McqQuestion') return '/questions'
  if (type === 'Mistake') return '/mistakes'
  if (type === 'WrittenNote') return '/written/notes'
  if (type === 'Topic' && topicId) return '/syllabus'
  if (type === 'CurrentAffairs' && sourceId) return `/current-affairs/${sourceId}`
  return null
}
