import { BookmarkCheck, ExternalLink } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { Field, Select, Textarea } from '../../components/ui/Form'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { mcqApi, type McqMistake, type McqMistakeStatus, type McqQuestion } from '../../services/mcq'

export function PersonalQuestionsPage({ kind }: { kind: 'bookmarks' | 'mistakes' }) {
  const { accessToken } = useAuth()
  const [questions, setQuestions] = useState<McqQuestion[]>([])
  const [mistakes, setMistakes] = useState<McqMistake[]>([])
  const [status, setStatus] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [savingId, setSavingId] = useState('')

  useEffect(() => {
    let active = true
    if (!accessToken) {
      setLoading(false)
      return () => { active = false }
    }

    const request = kind === 'bookmarks'
      ? mcqApi.getBookmarks({ pageNumber: 1, pageSize: 50 }, accessToken)
      : mcqApi.getMistakes({ pageNumber: 1, pageSize: 50, status: status || undefined }, accessToken)
    request
      .then((result) => {
        if (active && kind === 'bookmarks') setQuestions(result.items as McqQuestion[])
        if (active && kind === 'mistakes') setMistakes(result.items as McqMistake[])
      })
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load personal question data.'))
      .finally(() => active && setLoading(false))
    return () => { active = false }
  }, [accessToken, kind, status])

  async function removeBookmark(questionId: string) {
    if (!accessToken) return
    setSavingId(questionId)
    setError('')
    try {
      await mcqApi.removeBookmark(questionId, accessToken)
      setQuestions((current) => current.filter((question) => question.id !== questionId))
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to remove bookmark.')
    } finally {
      setSavingId('')
    }
  }

  async function updateMistake(mistake: McqMistake, nextStatus: McqMistakeStatus, notes: string) {
    if (!accessToken) return
    setSavingId(mistake.questionId)
    setError('')
    try {
      const updated = await mcqApi.updateMistake(mistake.questionId, nextStatus, notes || null, accessToken)
      setMistakes((current) => current.map((item) => item.questionId === updated.questionId ? updated : item)
        .filter((item) => !status || item.status === status))
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to update mistake status.')
    } finally {
      setSavingId('')
    }
  }

  const title = kind === 'bookmarks' ? 'Bookmarked questions' : 'Mistake review'
  return (
    <>
      <PageHeader title={title} description={kind === 'bookmarks' ? 'Questions saved for later review.' : 'Questions answered incorrectly are added here automatically after submission.'} action={<Link to="/questions" className="text-sm font-medium text-emerald-700 hover:underline">Question bank</Link>} />
      {kind === 'mistakes' && <div className="mb-5 max-w-xs"><Field label="Mistake status"><Select value={status} onChange={(event) => setStatus(event.target.value)}><option value="">All statuses</option><option value="Open">Open</option><option value="Reviewed">Reviewed</option><option value="Resolved">Resolved</option></Select></Field></div>}
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {!accessToken ? <Card><EmptyState title="Sign in to view personal questions" description="Bookmarks and mistakes are private to your account." /><div className="pb-5 text-center"><Link to="/login" className="text-sm font-medium text-emerald-700 hover:underline">Sign in</Link></div></Card>
        : loading ? <LoadingState label="Loading saved questions..." />
          : kind === 'bookmarks' ? questions.length === 0 ? <Card><EmptyState title="No bookmarks yet" description="Bookmark questions from the question bank to save them here." /></Card>
            : <div className="space-y-4">{questions.map((question) => <Card key={question.id} className="overflow-hidden"><CardHeader title={`${question.subjectName} · ${question.topicTitle}`} subtitle={question.examReference ?? question.difficulty} action={<Button variant="ghost" size="sm" disabled={savingId === question.id} onClick={() => void removeBookmark(question.id)} aria-label="Remove bookmark"><BookmarkCheck className="h-4 w-4 text-emerald-700" /></Button>} /><div className="space-y-3 p-5"><p className="text-sm leading-6">{question.prompt}</p><div className="grid gap-2 sm:grid-cols-2">{question.options.map((option, index) => <div key={option.id} className="rounded-lg border border-slate-200 px-3 py-2 text-sm dark:border-slate-700"><strong className="mr-2 text-slate-400">{String.fromCharCode(65 + index)}.</strong>{option.text}</div>)}</div><div className="flex flex-wrap gap-3 text-xs text-slate-500">{question.sources.map((source) => <a key={source.id} href={source.url} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1">{source.title}<ExternalLink className="h-3 w-3" /></a>)}</div></div></Card>)}</div>
            : mistakes.length === 0 ? <Card><EmptyState title="No mistakes to review" description="Incorrect answers from completed attempts will be tracked here." /></Card>
              : <div className="space-y-4">{mistakes.map((mistake) => <MistakeCard key={mistake.questionId} mistake={mistake} saving={savingId === mistake.questionId} onSave={updateMistake} />)}</div>}
    </>
  )
}

function MistakeCard({ mistake, saving, onSave }: { mistake: McqMistake; saving: boolean; onSave: (mistake: McqMistake, status: McqMistakeStatus, notes: string) => void }) {
  const [status, setStatus] = useState<McqMistakeStatus>(mistake.status)
  const [notes, setNotes] = useState(mistake.notes ?? '')
  return (
    <Card>
      <CardHeader title={mistake.topicTitle} subtitle={`${mistake.difficulty}${mistake.parentTopicTitle ? ` · ${mistake.parentTopicTitle}` : ''}`} action={<Badge>{mistake.status}</Badge>} />
      <div className="space-y-4 p-5">
        <p className="whitespace-pre-wrap text-sm leading-6">{mistake.prompt}</p>
        <div className="grid gap-4 sm:grid-cols-[180px_minmax(0,1fr)]">
          <Field label="Review status"><Select value={status} onChange={(event) => setStatus(event.target.value as McqMistakeStatus)}><option value="Open">Open</option><option value="Reviewed">Reviewed</option><option value="Resolved">Resolved</option></Select></Field>
          <Field label="Personal review note"><Textarea value={notes} onChange={(event) => setNotes(event.target.value)} maxLength={2000} placeholder="What should you remember next time?" /></Field>
        </div>
        <div className="flex flex-wrap items-center justify-between gap-3">
          {mistake.lastAttemptId ? <Link to={`/practice/attempts/${mistake.lastAttemptId}`} className="text-xs text-emerald-700 hover:underline">Open source attempt</Link> : <span />}
          <Button size="sm" disabled={saving} onClick={() => onSave(mistake, status, notes)}>{saving ? 'Saving...' : 'Save review'}</Button>
        </div>
      </div>
    </Card>
  )
}
