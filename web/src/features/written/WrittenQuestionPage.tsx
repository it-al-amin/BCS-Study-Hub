import { Bookmark, ExternalLink, FilePenLine } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { writtenApi, type WrittenModelAnswer, type WrittenQuestion } from '../../services/written'

export function WrittenQuestionPage() {
  const { questionId = '' } = useParams()
  const { accessToken } = useAuth()
  const navigate = useNavigate()
  const [question, setQuestion] = useState<WrittenQuestion | null>(null)
  const [modelAnswer, setModelAnswer] = useState<WrittenModelAnswer | null>(null)
  const [loading, setLoading] = useState(true)
  const [starting, setStarting] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    setLoading(true)
    writtenApi.getQuestion(questionId, accessToken ?? undefined)
      .then((result) => active && setQuestion(result))
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load this question.'))
      .finally(() => active && setLoading(false))
    return () => { active = false }
  }, [accessToken, questionId])

  useEffect(() => {
    if (!question?.hasModelAnswer) {
      setModelAnswer(null)
      return
    }
    let active = true
    writtenApi.getModelAnswer(question.id)
      .then((result) => active && setModelAnswer(result))
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load the model answer.'))
    return () => { active = false }
  }, [question])

  async function toggleBookmark() {
    if (!question) return
    if (!accessToken) {
      navigate('/login')
      return
    }
    setBusy(true)
    setError('')
    try {
      if (question.isBookmarked) await writtenApi.removeBookmark(question.id, accessToken)
      else await writtenApi.addBookmark(question.id, accessToken)
      setQuestion({ ...question, isBookmarked: !question.isBookmarked })
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to update this bookmark.')
    } finally {
      setBusy(false)
    }
  }

  async function startAnswer() {
    if (!question) return
    if (!accessToken) {
      navigate('/login')
      return
    }
    setStarting(true)
    setError('')
    try {
      const answer = await writtenApi.createAnswer(question.id, accessToken)
      navigate(`/written/answers/${answer.id}`)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to start an answer draft.')
    } finally {
      setStarting(false)
    }
  }

  return (
    <>
      <PageHeader title="Written question" description="Review attribution and guidance, then write a private draft. Self-assessment is not an official or automated evaluation." action={<Link to="/written" className="text-sm font-medium text-emerald-700 hover:underline">Back to Written preparation</Link>} />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {loading ? <LoadingState label="Loading Written question..." /> : !question ? <Card><EmptyState title="Question not found" description="This question may be unpublished or unavailable." /></Card> : (
        <div className="grid gap-5 xl:grid-cols-[minmax(0,1.3fr)_minmax(18rem,0.7fr)]">
          <div className="space-y-4">
            <Card className="p-5">
              <div className="flex flex-wrap items-center gap-2"><Badge>{question.type}</Badge>{question.bcsExamYear && <Badge>{question.bcsExamYear} BCS</Badge>}{question.examNumber && <Badge>Exam {question.examNumber}</Badge>}{question.marks !== null && <Badge>{question.marks} marks</Badge>}</div>
              <h2 className="mt-4 whitespace-pre-wrap text-lg font-semibold leading-8">{question.prompt}</h2>
              <p className="mt-3 text-sm text-slate-500">{question.subjectName}{question.paperName ? ` · ${question.paperName}` : ''} · {question.topicTitle}{question.parentTopicTitle ? ` / ${question.parentTopicTitle}` : ''} · {question.syllabusVersionLabel}</p>
              {question.examReference && <p className="mt-2 text-sm text-slate-500">Exam reference: {question.examReference}</p>}
              {question.answerGuidance && <div className="mt-5 rounded-lg bg-slate-50 p-4 dark:bg-slate-800/60"><h3 className="text-sm font-semibold">Answer-writing guidance</h3><p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-slate-600 dark:text-slate-300">{question.answerGuidance}</p></div>}
              <div className="mt-5 flex flex-wrap gap-2">
                {accessToken ? <Button disabled={busy} variant="secondary" onClick={() => void toggleBookmark()}><Bookmark className="h-4 w-4" />{question.isBookmarked ? 'Remove bookmark' : 'Bookmark'}</Button> : <Link to="/login" className="inline-flex h-10 items-center gap-2 rounded-lg border border-slate-200 px-4 text-sm font-medium"><Bookmark className="h-4 w-4" />Sign in to bookmark</Link>}
                <Button disabled={starting} onClick={() => void startAnswer()}><FilePenLine className="h-4 w-4" />{starting ? 'Starting...' : 'Write an answer'}</Button>
              </div>
            </Card>
            {question.hasModelAnswer && (
              <Card className="p-5">
                <h2 className="font-semibold">Published model answer</h2>
                <p className="mt-1 text-xs text-slate-500">Reference material for comparison; it is not an automatic evaluation of your writing.</p>
                {modelAnswer ? <div className="mt-4"><div className="whitespace-pre-wrap rounded-lg bg-slate-50 p-4 text-sm leading-7 dark:bg-slate-800/60">{modelAnswer.content}</div><h3 className="mt-4 text-sm font-semibold">Model-answer references</h3><ul className="mt-2 space-y-2">{modelAnswer.references.map((reference) => <li key={reference.id} className="text-sm"><a href={reference.url} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1 text-emerald-700 hover:underline dark:text-emerald-400">{reference.title}<ExternalLink className="h-3 w-3" /></a><span className="ml-2 text-xs text-slate-500">{reference.publisher}{reference.citation ? ` · ${reference.citation}` : ''}</span></li>)}</ul></div> : <LoadingState label="Loading model answer..." />}
              </Card>
            )}
          </div>
          <div className="space-y-4">
            <Card className="p-5">
              <h2 className="font-semibold">Question sources</h2>
              <ul className="mt-3 space-y-3">{question.sources.map((source) => <li key={source.id} className="border-b border-slate-100 pb-3 last:border-0 dark:border-slate-800"><a href={source.url} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1 text-sm font-medium text-emerald-700 hover:underline dark:text-emerald-400">{source.title}<ExternalLink className="h-3 w-3" /></a><p className="mt-1 text-xs text-slate-500">{source.publisher}{source.citation ? ` · ${source.citation}` : ''}{source.publishedOn ? ` · ${source.publishedOn}` : ''}</p></li>)}</ul>
            </Card>
            {question.resources.length > 0 && <Card className="p-5"><h2 className="font-semibold">Related resources</h2><ul className="mt-3 space-y-2">{question.resources.map((resource) => <li key={resource.id}><a href={resource.url} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1 text-sm text-emerald-700 hover:underline dark:text-emerald-400">{resource.title}<ExternalLink className="h-3 w-3" /></a></li>)}</ul></Card>}
          </div>
        </div>
      )}
    </>
  )
}
