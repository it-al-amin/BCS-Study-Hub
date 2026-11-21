import { Bookmark, FileText } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { ApiError } from '../../services/api'
import { vivaApi, type VivaAnswerNote, type VivaQuestion } from '../../services/viva'

export function VivaQuestionPage() {
  const { questionId = '' } = useParams()
  const { accessToken } = useAuth()
  const [question, setQuestion] = useState<VivaQuestion | null>(null)
  const [note, setNote] = useState<VivaAnswerNote | null>(null)
  const [noteText, setNoteText] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  useEffect(() => {
    let active = true
    vivaApi.getQuestion(questionId, accessToken ?? undefined)
      .then(async (result) => {
        if (!active) return
        setQuestion(result)
        if (!accessToken) return
        try {
          const savedNote = await vivaApi.getAnswerNote(questionId, accessToken)
          if (active) {
            setNote(savedNote)
            setNoteText(savedNote.content)
          }
        } catch (reason) {
          if (reason instanceof ApiError && reason.status === 404) {
            if (active) {
              setNote(null)
              setNoteText('')
            }
          } else {
            throw reason
          }
        }
      })
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load this Viva question.'))
      .finally(() => active && setLoading(false))
    return () => { active = false }
  }, [accessToken, questionId])

  async function toggleBookmark() {
    if (!question || !accessToken) return
    setError('')
    try {
      if (question.isBookmarked) await vivaApi.removeBookmark(question.id, accessToken)
      else await vivaApi.addBookmark(question.id, accessToken)
      setQuestion((current) => current ? { ...current, isBookmarked: !current.isBookmarked } : current)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to update the bookmark.')
    }
  }

  async function saveNote() {
    if (!question || !accessToken) return
    setSaving(true)
    setError('')
    setMessage('')
    try {
      const saved = await vivaApi.saveAnswerNote(question.id, noteText, accessToken)
      setNote(saved)
      setNoteText(saved.content)
      setMessage('Your private answer notes were saved.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to save personal answer notes.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <>
      <PageHeader title="Viva question" description="Practice answer notes are private to your account. Reference verification is shown as entered by a content editor." action={<Link to="/viva" className="text-sm font-medium text-emerald-700 hover:underline dark:text-emerald-400">Back to Viva preparation</Link>} />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {message && <p className="mb-4 rounded-lg bg-emerald-50 px-4 py-3 text-sm text-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-300" role="status">{message}</p>}
      {loading ? <LoadingState label="Loading Viva question..." /> : !question ? (
        <EmptyState title="Question not found" description="This question may have been archived or removed." />
      ) : (
        <div className="mx-auto max-w-4xl space-y-5">
          <Card className="p-5 sm:p-7">
            <div className="flex flex-wrap gap-2"><Badge>{question.categoryName}</Badge>{question.topic && <Badge>{question.topic}</Badge>}{question.bcsExamYear && <Badge>BCS {question.bcsExamYear}</Badge>}</div>
            <h2 className="mt-5 text-xl font-semibold leading-8 text-slate-950 dark:text-white">{question.prompt}</h2>
            {question.examReference && <p className="mt-2 text-sm text-slate-500">{question.examReference}{question.examNumber ? ` · Exam ${question.examNumber}` : ''}</p>}
            <Button className="mt-5" variant="secondary" disabled={!accessToken} onClick={() => void toggleBookmark()}><Bookmark className={`h-4 w-4 ${question.isBookmarked ? 'fill-emerald-600 text-emerald-600' : ''}`} />{question.isBookmarked ? 'Bookmarked' : 'Bookmark'}</Button>
          </Card>

          <Card>
            <CardHeader title="References and suggested reading" subtitle="These are references supplied by the editor; verify sources independently before relying on them." />
            {question.references.length === 0 ? <EmptyState title="No references supplied" description="No suggested sources have been attached to this question." /> : (
              <div className="divide-y divide-slate-100 dark:divide-slate-800">{question.references.map((reference) => <div key={reference.id} className="p-4 sm:p-5">
                <div className="flex flex-wrap items-center gap-2"><Badge>{reference.type === 'SuggestedAnswer' ? 'Suggested answer' : 'Question source'}</Badge><Badge>{reference.verificationStatus}</Badge></div>
                <a href={reference.url} target="_blank" rel="noreferrer" className="mt-2 inline-block text-sm font-medium text-emerald-700 hover:underline dark:text-emerald-400">{reference.title}</a>
                <p className="mt-1 text-xs text-slate-500">{reference.publisher}{reference.citation ? ` · ${reference.citation}` : ''}</p>
              </div>)}</div>
            )}
          </Card>

          <Card className="p-5">
            <CardHeader title="My answer notes" subtitle="Personal practice notes are not published or automatically evaluated." />
            {accessToken ? (
              <>
                <label htmlFor="viva-answer-note" className="mt-4 block text-sm font-medium">Your answer outline or reflection</label>
                <textarea id="viva-answer-note" value={noteText} onChange={(event) => setNoteText(event.target.value)} maxLength={20000} rows={10} placeholder="Write the points you want to practice..." className="mt-2 w-full rounded-lg border border-slate-200 bg-white p-3 text-sm leading-6 dark:border-slate-700 dark:bg-slate-950" />
                <div className="mt-2 flex flex-wrap items-center justify-between gap-3"><span className="text-xs text-slate-500">{noteText.length}/20,000 characters · saved revisions: {note?.revisions.length ?? 0}</span><Button disabled={saving} onClick={() => void saveNote()}><FileText className="h-4 w-4" />{saving ? 'Saving...' : 'Save notes'}</Button></div>
                {note && note.revisions.length > 0 && <div className="mt-5 border-t border-slate-100 pt-4 dark:border-slate-800"><h3 className="text-sm font-semibold">Revision history</h3><div className="mt-3 space-y-2">{note.revisions.slice(0, 5).map((revision) => <details key={revision.revisionNumber} className="rounded-lg bg-slate-50 p-3 text-sm dark:bg-slate-800"><summary className="cursor-pointer">Revision {revision.revisionNumber} · {new Date(revision.createdAt).toLocaleString()}</summary><p className="mt-2 whitespace-pre-wrap text-slate-600 dark:text-slate-300">{revision.content || 'Empty note'}</p></details>)}</div></div>}
              </>
            ) : <EmptyState title="Sign in to save answer notes" description="Your personal preparation notes and revisions require an account." />}
          </Card>
        </div>
      )}
    </>
  )
}
