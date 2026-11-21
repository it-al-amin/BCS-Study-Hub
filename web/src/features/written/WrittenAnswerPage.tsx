import { Clock3, Eye, FilePenLine, Save, Send } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { writtenApi, type WrittenAnswer, type WrittenAnswerRevision, type WrittenModelAnswer, type WrittenQuestion, type WrittenSelfAssessment } from '../../services/written'

const blankAssessment: WrittenSelfAssessment = {
  contentCoverage: null,
  organization: null,
  clarity: null,
  languageAccuracy: null,
  reflection: null,
}

function assessmentPayload(value: WrittenSelfAssessment) {
  return value.contentCoverage === null &&
    value.organization === null &&
    value.clarity === null &&
    value.languageAccuracy === null &&
    !value.reflection?.trim()
    ? null
    : value
}

function formatTime(seconds: number) {
  return `${Math.floor(seconds / 60)}m ${seconds % 60}s`
}

export function WrittenAnswerPage() {
  const { answerId = '' } = useParams()
  const { accessToken } = useAuth()
  const [answer, setAnswer] = useState<WrittenAnswer | null>(null)
  const [content, setContent] = useState('')
  const [assessment, setAssessment] = useState<WrittenSelfAssessment>(blankAssessment)
  const [elapsedSeconds, setElapsedSeconds] = useState(0)
  const [revisions, setRevisions] = useState<WrittenAnswerRevision[]>([])
  const [question, setQuestion] = useState<WrittenQuestion | null>(null)
  const [modelAnswer, setModelAnswer] = useState<WrittenModelAnswer | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const currentAnswerId = answer?.id
  const currentAnswerStatus = answer?.status

  useEffect(() => {
    if (!accessToken) {
      setLoading(false)
      return
    }
    let active = true
    setLoading(true)
    Promise.all([
      writtenApi.getAnswer(answerId, accessToken),
      writtenApi.getRevisions(answerId, accessToken),
    ])
      .then(([answerResult, revisionResult]) => {
        if (!active) return
        setAnswer(answerResult)
        setContent(answerResult.content)
        setAssessment(answerResult.selfAssessment ?? blankAssessment)
        setRevisions(revisionResult)
      })
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load this private answer.'))
      .finally(() => active && setLoading(false))
    return () => { active = false }
  }, [accessToken, answerId])

  useEffect(() => {
    if (!answer || !accessToken) return
    let active = true
    writtenApi.getQuestion(answer.questionId, accessToken)
      .then(async (result) => {
        if (!active) return
        setQuestion(result)
        if (result.hasModelAnswer) {
          const model = await writtenApi.getModelAnswer(result.id)
          if (active) setModelAnswer(model)
        }
      })
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load question references.'))
    return () => { active = false }
  }, [accessToken, answer])

  useEffect(() => {
    if (!currentAnswerId || currentAnswerStatus !== 'Draft') return
    const timer = window.setInterval(() => setElapsedSeconds((current) => current + 1), 1000)
    return () => window.clearInterval(timer)
  }, [currentAnswerId, currentAnswerStatus])

  async function reloadRevisions(token: string) {
    setRevisions(await writtenApi.getRevisions(answerId, token))
  }

  async function saveDraft() {
    if (!answer || !accessToken || answer.status !== 'Draft') return
    setSaving(true)
    setError('')
    setMessage('')
    try {
      const saved = await writtenApi.updateAnswer(
        answer.id,
        content,
        answer.timeSpentSeconds + elapsedSeconds,
        assessmentPayload(assessment),
        accessToken,
      )
      setAnswer(saved)
      setContent(saved.content)
      setElapsedSeconds(0)
      await reloadRevisions(accessToken)
      setMessage('Draft saved; word count and revision history were updated.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to save the answer draft.')
    } finally {
      setSaving(false)
    }
  }

  async function submitAnswer() {
    if (!answer || !accessToken || answer.status !== 'Draft') return
    if (!window.confirm('Submit this answer? You can start a new revision later, but the submitted version will remain in history.')) return
    setSaving(true)
    setError('')
    setMessage('')
    try {
      const submitted = await writtenApi.submitAnswer(
        answer.id,
        content,
        answer.timeSpentSeconds + elapsedSeconds,
        assessmentPayload(assessment),
        accessToken,
      )
      setAnswer(submitted)
      setContent(submitted.content)
      setElapsedSeconds(0)
      await reloadRevisions(accessToken)
      setMessage('Answer submitted and recorded in your private history.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to submit the answer.')
    } finally {
      setSaving(false)
    }
  }

  async function startRevision() {
    if (!answer || !accessToken) return
    setSaving(true)
    setError('')
    try {
      const revised = await writtenApi.reviseAnswer(answer.id, accessToken)
      setAnswer(revised)
      setContent(revised.content)
      setAssessment(revised.selfAssessment ?? blankAssessment)
      await reloadRevisions(accessToken)
      setMessage('A new draft revision has been started.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to start a new revision.')
    } finally {
      setSaving(false)
    }
  }

  function updateRating(field: keyof WrittenSelfAssessment, value: string) {
    setAssessment((current) => ({
      ...current,
      [field]: field === 'reflection' ? value : value === '' ? null : Number(value),
    }))
  }

  const isDraft = answer?.status === 'Draft'
  const currentWordCount = content.trim() ? content.trim().split(/\s+/).length : 0

  return (
    <>
      <PageHeader title="Answer-writing workspace" description="Your writing is private. Word count is calculated by the server; time spent is self-reported from this browser session." action={<Link to="/written/answers" className="text-sm font-medium text-emerald-700 hover:underline">My answers</Link>} />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {message && <p className="mb-4 rounded-lg bg-emerald-50 px-4 py-3 text-sm text-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-300" role="status">{message}</p>}
      {!accessToken ? <Card><EmptyState title="Sign in to access your answer" description="Personal writing drafts and submissions are private." /></Card>
        : loading ? <LoadingState label="Loading your answer..." />
          : !answer ? <Card><EmptyState title="Answer not found" description="This answer may have been removed or belongs to another account." /></Card>
            : (
              <div className="grid gap-5 xl:grid-cols-[minmax(0,1.35fr)_minmax(18rem,0.65fr)]">
                <div className="space-y-4">
                  <Card className="p-5">
                    <div className="flex flex-wrap items-center gap-2"><Badge>{answer.questionType}</Badge><Badge>{answer.status}</Badge>{question?.marks !== null && question?.marks !== undefined && <Badge>{question.marks} marks</Badge>}</div>
                    <h2 className="mt-4 whitespace-pre-wrap text-lg font-semibold leading-8">{answer.questionPrompt}</h2>
                    <p className="mt-2 text-sm text-slate-500">{answer.topicTitle}</p>
                    {isDraft ? (
                      <>
                        <label htmlFor="written-answer-content" className="mt-5 block text-sm font-medium">Your answer</label>
                        <textarea id="written-answer-content" value={content} onChange={(event) => setContent(event.target.value)} maxLength={50000} rows={18} className="mt-2 w-full resize-y rounded-lg border border-slate-200 bg-white p-3 text-sm leading-7 outline-none focus:border-emerald-600 focus:ring-2 focus:ring-emerald-600/20 dark:border-slate-700 dark:bg-slate-950" placeholder="Write your answer here. Save a draft to preserve this revision." />
                        <div className="mt-2 flex flex-wrap items-center justify-between gap-2 text-xs text-slate-500"><span>{currentWordCount} words · {content.length}/50,000 characters</span><span className="inline-flex items-center gap-1"><Clock3 className="h-3.5 w-3.5" />{formatTime(answer.timeSpentSeconds + elapsedSeconds)}</span></div>
                        <div className="mt-5 rounded-lg border border-slate-200 p-4 dark:border-slate-700">
                          <h3 className="font-semibold">Personal self-assessment</h3>
                          <p className="mt-1 text-xs text-slate-500">Optional, self-entered reflection only; no automatic or AI evaluation is performed.</p>
                          <div className="mt-4 grid gap-3 sm:grid-cols-2">
                            {([
                              ['contentCoverage', 'Content coverage'],
                              ['organization', 'Organization'],
                              ['clarity', 'Clarity'],
                              ['languageAccuracy', 'Language accuracy'],
                            ] as const).map(([field, label]) => <label key={field} className="text-xs font-medium text-slate-600 dark:text-slate-300">{label}<select value={assessment[field] ?? ''} onChange={(event) => updateRating(field, event.target.value)} className="mt-1 block h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950"><option value="">Not rated</option>{[1, 2, 3, 4, 5].map((score) => <option key={score} value={score}>{score} / 5</option>)}</select></label>)}
                          </div>
                          <label htmlFor="written-reflection" className="mt-3 block text-xs font-medium text-slate-600 dark:text-slate-300">Reflection<textarea id="written-reflection" value={assessment.reflection ?? ''} onChange={(event) => updateRating('reflection', event.target.value)} maxLength={2000} rows={3} className="mt-1 block w-full rounded-lg border border-slate-200 bg-white p-2 text-sm dark:border-slate-700 dark:bg-slate-950" /></label>
                        </div>
                        <div className="mt-4 flex flex-wrap justify-end gap-2"><Button variant="secondary" disabled={saving || !content.trim()} onClick={() => void saveDraft()}><Save className="h-4 w-4" />{saving ? 'Saving...' : 'Save draft'}</Button><Button disabled={saving || !content.trim()} onClick={() => void submitAnswer()}><Send className="h-4 w-4" />Submit answer</Button></div>
                      </>
                    ) : (
                      <>
                        <div className="mt-5 whitespace-pre-wrap rounded-lg bg-slate-50 p-4 text-sm leading-7 dark:bg-slate-800/60">{answer.content}</div>
                        <div className="mt-3 flex flex-wrap items-center justify-between gap-2 text-xs text-slate-500"><span>{answer.wordCount} words · {formatTime(answer.timeSpentSeconds)} writing time</span><span>Submitted {answer.submittedAt ? new Date(answer.submittedAt).toLocaleString() : '—'}</span></div>
                        {answer.selfAssessment && <div className="mt-4 rounded-lg border border-slate-200 p-4 text-sm dark:border-slate-700"><h3 className="font-semibold">Your self-assessment</h3><p className="mt-2 text-xs text-slate-500">Personal reflection, not an official score or automated evaluation.</p><p className="mt-2 text-sm text-slate-600 dark:text-slate-300">Coverage {answer.selfAssessment.contentCoverage ?? '—'} · Organization {answer.selfAssessment.organization ?? '—'} · Clarity {answer.selfAssessment.clarity ?? '—'} · Language {answer.selfAssessment.languageAccuracy ?? '—'}</p>{answer.selfAssessment.reflection && <p className="mt-2 whitespace-pre-wrap text-sm text-slate-600 dark:text-slate-300">{answer.selfAssessment.reflection}</p>}</div>}
                        <div className="mt-4 flex justify-end"><Button variant="secondary" disabled={saving} onClick={() => void startRevision()}><FilePenLine className="h-4 w-4" />{saving ? 'Starting...' : 'Start a revision'}</Button></div>
                      </>
                    )}
                  </Card>
                  {modelAnswer && <Card className="p-5"><h2 className="font-semibold">Model answer comparison</h2><p className="mt-1 text-xs text-slate-500">Compare manually; this system does not automatically score your answer.</p><div className="mt-3 whitespace-pre-wrap rounded-lg bg-slate-50 p-4 text-sm leading-7 dark:bg-slate-800/60">{modelAnswer.content}</div></Card>}
                </div>
                <aside className="space-y-4">
                  <Card className="p-5"><h2 className="font-semibold">Revision history</h2>{revisions.length === 0 ? <p className="mt-2 text-sm text-slate-500">Save a draft to create the first revision record.</p> : <ol className="mt-3 space-y-3">{[...revisions].reverse().map((revision) => <li key={revision.revisionNumber} className="border-b border-slate-100 pb-3 last:border-0 dark:border-slate-800"><div className="flex items-center justify-between gap-2"><Badge>{revision.kind}</Badge><span className="text-[11px] text-slate-500">#{revision.revisionNumber}</span></div><p className="mt-1 text-xs text-slate-500">{new Date(revision.createdAt).toLocaleString()} · {revision.wordCount} words</p><details className="mt-2"><summary className="flex cursor-pointer items-center gap-1 text-xs text-emerald-700 hover:underline"><Eye className="h-3 w-3" />View snapshot</summary><pre className="mt-2 max-h-64 overflow-auto whitespace-pre-wrap rounded bg-slate-50 p-2 text-xs leading-5 dark:bg-slate-800">{revision.content}</pre></details></li>)}</ol>}</Card>
                  {question?.answerGuidance && <Card className="p-5"><h2 className="font-semibold">Answer-writing guidance</h2><p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-slate-600 dark:text-slate-300">{question.answerGuidance}</p></Card>}
                </aside>
              </div>
            )}
    </>
  )
}
