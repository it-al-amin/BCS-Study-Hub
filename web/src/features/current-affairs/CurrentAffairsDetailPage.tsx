import { ArrowLeft, Bookmark, BookmarkPlus, ExternalLink, FileText, Image as ImageIcon, Link2, Printer, RotateCcw, Save, Trash2 } from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { ApiError } from '../../services/api'
import { currentAffairsApi, type CurrentAffairsDetail, type QuestionType, type RevisionOutcome } from '../../services/currentAffairs'
import { categoryLabels, errorMessage, formatDate, verificationTone } from './currentAffairsFormat'
import { Markdown } from '../../components/content/Markdown'

const questionLabels: Record<QuestionType, string> = { Mcq: 'MCQ', Written: 'Written', Viva: 'Viva' }

function questionLink(type: QuestionType, id: string) {
  if (type === 'Written') return `/written/questions/${id}`
  if (type === 'Viva') return `/viva/questions/${id}`
  return '/questions'
}

export function CurrentAffairsDetailPage() {
  const { entryId = '' } = useParams()
  return <EntryDetail key={entryId} entryId={entryId} />
}

function EntryDetail({ entryId }: { entryId: string }) {
  const { accessToken } = useAuth()
  const [detail, setDetail] = useState<CurrentAffairsDetail | null>(null)
  const [notFound, setNotFound] = useState(false)
  const [error, setError] = useState('')
  const [noteDraft, setNoteDraft] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const reload = useCallback(async () => {
    setDetail(await currentAffairsApi.getEntry(entryId, accessToken))
  }, [entryId, accessToken])

  useEffect(() => {
    let active = true
    currentAffairsApi.getEntry(entryId, accessToken)
      .then((result) => active && setDetail(result))
      .catch((reason: unknown) => {
        if (!active) return
        if (reason instanceof ApiError && reason.status === 404) setNotFound(true)
        else setError(errorMessage(reason, 'Unable to load this entry.'))
      })
    return () => { active = false }
  }, [entryId, accessToken])

  async function run(action: () => Promise<unknown>) {
    setBusy(true)
    setError('')
    try {
      await action()
      await reload()
    } catch (reason) {
      setError(errorMessage(reason, 'The action could not be completed.'))
    } finally {
      setBusy(false)
    }
  }

  if (notFound) return <Card><EmptyState title="Entry not found" description="It may be unpublished or archived." /></Card>
  if (!detail) return error ? <ErrorState description={error} /> : <LoadingState label="Loading entry..." />

  const { entry } = detail
  const note = noteDraft ?? detail.myNote?.content ?? ''

  return (
    <article className="mx-auto max-w-4xl">
      <div className="mb-4 flex items-center justify-between print:hidden">
        <Link to="/current-affairs" className="inline-flex items-center gap-1 text-sm text-slate-500 hover:text-slate-800 dark:hover:text-slate-200"><ArrowLeft className="h-4 w-4" />Current affairs</Link>
        <Button size="sm" variant="ghost" onClick={() => window.print()}><Printer className="h-4 w-4" />Print</Button>
      </div>
      <div className="flex flex-wrap items-center gap-2 text-sm">
        <time dateTime={entry.eventDate} className="font-medium">{formatDate(entry.eventDate)}</time>
        <Link to={`/current-affairs?category=${entry.category}`}><Badge tone="info">{categoryLabels[entry.category]}</Badge></Link>
        {entry.sourcePublishedOn && <span className="text-xs text-slate-500">Source published {formatDate(entry.sourcePublishedOn)}</span>}
      </div>
      <h1 className="mt-2 text-2xl font-semibold tracking-tight">{entry.title}</h1>
      <p className="mt-3 text-base leading-7 text-slate-700 dark:text-slate-200">{entry.summary}</p>

      {accessToken ? <div className="mt-4 flex flex-wrap gap-2 print:hidden">
        <Button size="sm" variant="secondary" disabled={busy} onClick={() => void run(() => entry.isBookmarked ? currentAffairsApi.removeBookmark(entry.id, accessToken) : currentAffairsApi.addBookmark(entry.id, accessToken))}>{entry.isBookmarked ? <><Bookmark className="h-4 w-4 fill-current" />Bookmarked</> : <><BookmarkPlus className="h-4 w-4" />Bookmark</>}</Button>
        {detail.myRevision
          ? <Button size="sm" variant="secondary" disabled={busy} onClick={() => void run(() => currentAffairsApi.removeRevision(entry.id, accessToken))}><RotateCcw className="h-4 w-4" />Remove from revision</Button>
          : <Button size="sm" variant="secondary" disabled={busy} onClick={() => void run(() => currentAffairsApi.addRevision(entry.id, accessToken))}><RotateCcw className="h-4 w-4" />Add to revision</Button>}
      </div> : <p className="mt-4 text-sm text-slate-500 print:hidden"><Link to="/login" className="text-emerald-700 hover:underline dark:text-emerald-400">Sign in</Link> to bookmark, take notes, and schedule revision.</p>}
      {error && <div className="mt-4"><ErrorState description={error} /></div>}

      {detail.myRevision && <Card className="mt-5 p-4 print:hidden">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <p className="text-sm">{detail.myRevision.isDue ? <strong>Due for revision now.</strong> : <>Next review on <strong>{formatDate(detail.myRevision.nextReviewOn)}</strong>.</>} <span className="text-xs text-slate-500">Reviewed {detail.myRevision.reviewCount} time{detail.myRevision.reviewCount === 1 ? '' : 's'}.</span></p>
          <div className="flex gap-1">{(['Again', 'Hard', 'Good', 'Easy'] as RevisionOutcome[]).map((outcome) => <Button key={outcome} size="sm" variant={outcome === 'Good' ? 'primary' : 'secondary'} disabled={busy || !accessToken} onClick={() => void run(() => currentAffairsApi.review(entry.id, outcome, accessToken!))}>{outcome}</Button>)}</div>
        </div>
      </Card>}

      {detail.content && <Markdown source={detail.content} className="mt-6" />}
      {detail.notes && <aside className="mt-6 rounded-lg border-l-4 border-amber-400 bg-amber-50 p-4 text-sm dark:bg-amber-950/20"><p className="text-xs font-semibold uppercase tracking-wide text-amber-800 dark:text-amber-300">Editor's note</p><p className="mt-1 whitespace-pre-wrap">{detail.notes}</p></aside>}

      <section className="mt-6 grid gap-4 md:grid-cols-2">
        <Card className="p-5">
          <h2 className="font-semibold">Source</h2>
          <a href={entry.sourceUrl} target="_blank" rel="noopener noreferrer nofollow" className="mt-2 inline-flex items-start gap-1 break-all text-sm text-emerald-700 hover:underline dark:text-emerald-400"><ExternalLink className="mt-0.5 h-4 w-4 shrink-0" />{entry.sourceName}</a>
          <p className="mt-1 break-all text-xs text-slate-500">{entry.sourceUrl}</p>
          {detail.officialSource && <p className="mt-3 text-xs text-slate-500">Directory: <Link to="/official-sources" className="hover:underline">{detail.officialSource.name}</Link> <Badge tone={verificationTone[detail.officialSource.verificationStatus]}>{detail.officialSource.verificationStatus}{detail.officialSource.lastVerifiedOn ? ` ${formatDate(detail.officialSource.lastVerifiedOn)}` : ''}</Badge></p>}
          {detail.references.length > 0 && <><h3 className="mt-4 text-sm font-medium">References</h3><ul className="mt-1 space-y-1">{detail.references.map((reference) => <li key={reference.id}><a href={reference.url} target="_blank" rel="noopener noreferrer nofollow" className="inline-flex items-center gap-1 text-sm hover:underline">{reference.kind === 'Pdf' ? <FileText className="h-4 w-4 text-red-500" /> : reference.kind === 'Image' ? <ImageIcon className="h-4 w-4 text-violet-500" /> : <Link2 className="h-4 w-4 text-sky-500" />}{reference.title}</a></li>)}</ul></>}
          {detail.libraryResources.length > 0 && <><h3 className="mt-4 text-sm font-medium">In the library</h3><ul className="mt-1 space-y-1">{detail.libraryResources.map((resource) => <li key={resource.id}><Link to={`/library/resources/${resource.id}`} className="text-sm text-emerald-700 hover:underline dark:text-emerald-400">{resource.title}</Link> <span className="text-xs text-slate-500">{resource.primaryFormat ?? resource.kind}</span></li>)}</ul><p className="mt-1 text-xs text-slate-500 print:hidden">Library files require sign-in.</p></>}
        </Card>
        <Card className="p-5">
          <h2 className="font-semibold">Syllabus links</h2>
          {entry.subjects.length === 0 && entry.topics.length === 0 && <p className="mt-2 text-sm text-slate-500">Not mapped to the syllabus.</p>}
          <div className="mt-2 flex flex-wrap gap-1.5">
            {entry.subjects.map((subject) => <Link key={subject.id} to={`/current-affairs?subjectId=${subject.id}`} className="rounded-full bg-emerald-50 px-2.5 py-1 text-xs text-emerald-800 hover:underline dark:bg-emerald-500/10 dark:text-emerald-300">{subject.name}</Link>)}
            {entry.topics.map((topic) => <Link key={topic.id} to={`/current-affairs?syllabusItemId=${topic.id}`} title={`${topic.subjectName} · ${topic.stageCode} ${topic.versionLabel}`} className="rounded-full bg-sky-50 px-2.5 py-1 text-xs text-sky-800 hover:underline dark:bg-sky-500/10 dark:text-sky-300">{topic.title}</Link>)}
          </div>
          {entry.topics.length > 0 && <p className="mt-2 text-xs text-slate-500 print:hidden">Select a topic to see other current affairs on it, or open the <Link to="/syllabus" className="hover:underline">syllabus</Link>.</p>}
          {detail.questions.length > 0 && <><h3 className="mt-4 text-sm font-medium">Related questions</h3><ul className="mt-1 space-y-2">{detail.questions.map((question) => <li key={`${question.type}-${question.id}`} className="text-sm"><Badge tone="neutral">{questionLabels[question.type]}</Badge> <Link to={questionLink(question.type, question.id)} className="hover:underline">{question.prompt}</Link></li>)}</ul></>}
          {entry.tags.length > 0 && <div className="mt-4 flex flex-wrap gap-1">{entry.tags.map((tag) => <Link key={tag} to={`/current-affairs?tag=${encodeURIComponent(tag)}`} className="rounded bg-slate-100 px-2 py-0.5 text-xs hover:underline dark:bg-slate-800">{tag}</Link>)}</div>}
        </Card>
      </section>

      {accessToken && <Card className="mt-5 p-5 print:hidden">
        <h2 className="font-semibold">My study note</h2>
        <p className="text-xs text-slate-500">Private to you.</p>
        <textarea aria-label="Study note" value={note} onChange={(event) => setNoteDraft(event.target.value)} maxLength={20000} rows={5} placeholder="Key facts, figures, and exam angles…" className="mt-3 w-full rounded-lg border border-slate-200 bg-white p-3 text-sm dark:border-slate-700 dark:bg-slate-950" />
        <div className="mt-2 flex gap-2">
          <Button size="sm" disabled={busy || !note.trim() || note === detail.myNote?.content} onClick={() => void run(async () => { await currentAffairsApi.saveNote(entry.id, note, accessToken); setNoteDraft(null) })}><Save className="h-4 w-4" />Save note</Button>
          {detail.myNote && <Button size="sm" variant="ghost" disabled={busy} onClick={() => window.confirm('Delete your note?') && void run(async () => { await currentAffairsApi.deleteNote(entry.id, accessToken); setNoteDraft(null) })}><Trash2 className="h-4 w-4" />Delete</Button>}
        </div>
      </Card>}
      {detail.myNote && <section className="mt-5 hidden print:block"><h2 className="font-semibold">My note</h2><p className="whitespace-pre-wrap text-sm">{detail.myNote.content}</p></section>}
    </article>
  )
}
