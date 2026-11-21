import { ChevronLeft, ChevronRight, Flag, Save } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { vivaApi, type VivaMockSession } from '../../services/viva'

type QuestionEdit = { answerNotes: string; needsRevision: boolean }
type Evaluation = {
  communicationRating: number | null
  subjectKnowledgeRating: number | null
  clarityRating: number | null
  confidenceRating: number | null
  feedbackNotes: string
  improvementAreas: string
}

const blankEvaluation: Evaluation = {
  communicationRating: null,
  subjectKnowledgeRating: null,
  clarityRating: null,
  confidenceRating: null,
  feedbackNotes: '',
  improvementAreas: '',
}

const ratingFields: Array<{ key: keyof Pick<Evaluation, 'communicationRating' | 'subjectKnowledgeRating' | 'clarityRating' | 'confidenceRating'>; label: string }> = [
  { key: 'communicationRating', label: 'Communication' },
  { key: 'subjectKnowledgeRating', label: 'Subject knowledge' },
  { key: 'clarityRating', label: 'Clarity' },
  { key: 'confidenceRating', label: 'Confidence' },
]

export function VivaSessionPage() {
  const { sessionId = '' } = useParams()
  const { accessToken } = useAuth()
  const [session, setSession] = useState<VivaMockSession | null>(null)
  const [edits, setEdits] = useState<Record<string, QuestionEdit>>({})
  const [evaluation, setEvaluation] = useState<Evaluation>(blankEvaluation)
  const [questionIndex, setQuestionIndex] = useState(0)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  useEffect(() => {
    if (!accessToken) return
    let active = true
    vivaApi.getSession(sessionId, accessToken)
      .then((result) => active && setSession(result))
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load mock Viva session.'))
      .finally(() => active && setLoading(false))
    return () => { active = false }
  }, [accessToken, sessionId])

  const currentQuestion = session?.questions[questionIndex]
  const currentEdit = currentQuestion
    ? edits[currentQuestion.id] ?? { answerNotes: currentQuestion.answerNotes, needsRevision: currentQuestion.needsRevision }
    : null
  const isComplete = session?.status === 'Completed'

  function updateCurrent(patch: Partial<QuestionEdit>) {
    if (!currentQuestion || !currentEdit) return
    setEdits((current) => ({
      ...current,
      [currentQuestion.id]: { ...currentEdit, ...patch },
    }))
  }

  async function saveQuestion(questionId: string) {
    if (!accessToken || !session) return
    const question = session.questions.find((item) => item.id === questionId)
    if (!question) return
    const edit = edits[questionId] ?? { answerNotes: question.answerNotes, needsRevision: question.needsRevision }
    setSaving(true)
    setError('')
    setMessage('')
    try {
      const saved = await vivaApi.updateSessionQuestion(session.id, questionId, edit.answerNotes, edit.needsRevision, accessToken)
      setSession(saved)
      setEdits((current) => {
        const next = { ...current }
        delete next[questionId]
        return next
      })
      setMessage('This session answer note was saved.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to save this session question.')
    } finally {
      setSaving(false)
    }
  }

  async function completeSession() {
    if (!accessToken || !session) return
    setSaving(true)
    setError('')
    setMessage('')
    try {
      for (const question of session.questions) {
        const edit = edits[question.id]
        if (edit && (edit.answerNotes !== question.answerNotes || edit.needsRevision !== question.needsRevision)) {
          await vivaApi.updateSessionQuestion(session.id, question.id, edit.answerNotes, edit.needsRevision, accessToken)
        }
      }
      const completed = await vivaApi.completeSession(session.id, evaluation, accessToken)
      setSession(completed)
      setEdits({})
      setMessage('Mock Viva completed. Your feedback and improvement areas were saved.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to complete mock Viva.')
    } finally {
      setSaving(false)
    }
  }

  const finishedTimestamp = session?.completedAt
    ? new Date(session.completedAt).toLocaleString()
    : ''

  return (
    <>
      <PageHeader title={session?.title ?? 'Mock Viva session'} description="Write your answer outline for each prompt, flag topics to revisit, then record your own evaluation. No recording or automated scoring is performed." action={<Link to="/viva" className="text-sm font-medium text-emerald-700 hover:underline dark:text-emerald-400">Back to Viva</Link>} />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {message && <p className="mb-4 rounded-lg bg-emerald-50 px-4 py-3 text-sm text-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-300" role="status">{message}</p>}
      {!accessToken ? (
        <EmptyState title="Sign in required" description="Mock Viva sessions and evaluations are private to your account." />
      ) : loading ? <LoadingState label="Loading mock Viva..." /> : !session ? (
        <EmptyState title="Session not found" description="This private mock Viva session may not belong to your account." />
      ) : (
        <div className="mx-auto max-w-4xl space-y-5">
          <Card className="p-4">
            <div className="flex flex-wrap items-center justify-between gap-2"><div><Badge>{session.status === 'Completed' ? 'Completed' : 'In progress'}</Badge><span className="ml-2 text-sm text-slate-500">{session.questions.length} questions · started {new Date(session.startedAt).toLocaleString()}</span></div>{finishedTimestamp && <span className="text-xs text-slate-500">Completed {finishedTimestamp}</span>}</div>
            <div className="mt-4 flex flex-wrap gap-2">{session.questions.map((question, index) => <button key={question.id} type="button" onClick={() => setQuestionIndex(index)} className={`grid h-9 w-9 place-items-center rounded-lg border text-sm ${index === questionIndex ? 'border-emerald-700 bg-emerald-700 text-white' : 'border-slate-200 dark:border-slate-700'}`}>{index + 1}</button>)}</div>
          </Card>

          {currentQuestion && currentEdit ? (
            <Card className="p-5 sm:p-7">
              <div className="flex items-start justify-between gap-3"><div className="flex flex-wrap gap-2"><Badge>{currentQuestion.categoryName}</Badge>{currentQuestion.topic && <Badge>{currentQuestion.topic}</Badge>}</div><span className="text-xs text-slate-500">Question {questionIndex + 1} of {session.questions.length}</span></div>
              <h2 className="mt-5 text-lg font-semibold leading-8">{currentQuestion.prompt}</h2>
              <label htmlFor="viva-session-answer" className="mt-5 block text-sm font-medium">Your answer notes</label>
              <textarea id="viva-session-answer" disabled={isComplete} maxLength={20000} rows={10} value={currentEdit.answerNotes} onChange={(event) => updateCurrent({ answerNotes: event.target.value })} placeholder="Write your answer outline and points to discuss..." className="mt-2 w-full rounded-lg border border-slate-200 bg-white p-3 text-sm leading-6 disabled:bg-slate-50 dark:border-slate-700 dark:bg-slate-950 dark:disabled:bg-slate-900" />
              <label className="mt-3 flex items-center gap-2 text-sm"><input type="checkbox" checked={currentEdit.needsRevision} disabled={isComplete} onChange={(event) => updateCurrent({ needsRevision: event.target.checked })} className="h-4 w-4 accent-emerald-700" /><Flag className="h-4 w-4 text-amber-600" />Flag for revision</label>
              {!isComplete && <div className="mt-4 flex flex-wrap items-center justify-between gap-2"><Button variant="secondary" disabled={questionIndex === 0} onClick={() => setQuestionIndex((index) => index - 1)}><ChevronLeft className="h-4 w-4" />Previous</Button><div className="flex gap-2"><Button variant="secondary" disabled={saving} onClick={() => void saveQuestion(currentQuestion.id)}><Save className="h-4 w-4" />Save answer</Button><Button disabled={questionIndex === session.questions.length - 1} onClick={() => setQuestionIndex((index) => index + 1)}>Next<ChevronRight className="h-4 w-4" /></Button></div></div>}
            </Card>
          ) : null}

          {!isComplete && (
            <Card className="p-5">
              <h2 className="font-semibold">Self-evaluation and feedback</h2>
              <p className="mt-1 text-sm text-slate-500">Optional 1–5 ratings are your own reflection, not an official score.</p>
              <div className="mt-4 grid gap-3 sm:grid-cols-2">{ratingFields.map((field) => <label key={field.key} className="text-xs font-medium text-slate-500">{field.label}<select value={evaluation[field.key] ?? ''} onChange={(event) => setEvaluation((current) => ({ ...current, [field.key]: event.target.value ? Number(event.target.value) : null }))} className="mt-1 h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-950"><option value="">Not rated</option>{[1, 2, 3, 4, 5].map((rating) => <option key={rating} value={rating}>{rating}</option>)}</select></label>)}</div>
              <div className="mt-3 grid gap-3 sm:grid-cols-2"><label className="text-xs font-medium text-slate-500">Feedback notes<textarea rows={3} maxLength={4000} value={evaluation.feedbackNotes} onChange={(event) => setEvaluation((current) => ({ ...current, feedbackNotes: event.target.value }))} className="mt-1 w-full rounded-lg border border-slate-200 bg-white p-3 text-sm dark:border-slate-700 dark:bg-slate-950" /></label><label className="text-xs font-medium text-slate-500">Improvement areas<textarea rows={3} maxLength={4000} value={evaluation.improvementAreas} onChange={(event) => setEvaluation((current) => ({ ...current, improvementAreas: event.target.value }))} className="mt-1 w-full rounded-lg border border-slate-200 bg-white p-3 text-sm dark:border-slate-700 dark:bg-slate-950" /></label></div>
              <Button className="mt-4" disabled={saving} onClick={() => void completeSession()}>{saving ? 'Saving...' : 'Complete session'}</Button>
            </Card>
          )}

          {isComplete && (
            <Card className="p-5">
              <h2 className="font-semibold">Your evaluation</h2>
              <div className="mt-3 grid grid-cols-2 gap-3 text-sm sm:grid-cols-4">{ratingFields.map((field) => <div key={field.key} className="rounded-lg bg-slate-50 p-3 dark:bg-slate-800"><p className="text-xs text-slate-500">{field.label}</p><p className="mt-1 font-semibold">{session[field.key] ?? '—'}{session[field.key] ? '/5' : ''}</p></div>)}</div>
              {session.feedbackNotes && <p className="mt-4 whitespace-pre-wrap text-sm"><strong>Feedback:</strong> {session.feedbackNotes}</p>}
              {session.improvementAreas && <p className="mt-3 whitespace-pre-wrap text-sm"><strong>Improvement areas:</strong> {session.improvementAreas}</p>}
              <p className="mt-4 text-xs text-slate-500">This session is read-only. You can create another session to practice again.</p>
            </Card>
          )}
        </div>
      )}
    </>
  )
}
