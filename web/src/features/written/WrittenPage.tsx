import { Bookmark, BookOpen, FileText, Target } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { syllabusApi, type SyllabusItem, type SyllabusTree, type SyllabusVersion } from '../../services/syllabus'
import { writtenApi, type WrittenAnswerListItem, type WrittenGoal, type WrittenQuestion, type WrittenQuestionType } from '../../services/written'

const questionTypes: WrittenQuestionType[] = ['Descriptive', 'Analytical', 'Essay', 'Composition', 'Translation']

function TopicList({
  items,
  selectedItemId,
  onSelect,
  depth = 0,
}: {
  items: SyllabusItem[]
  selectedItemId: string
  onSelect: (id: string) => void
  depth?: number
}) {
  return items.map((item) => (
    <div key={item.id}>
      <button
        type="button"
        onClick={() => onSelect(selectedItemId === item.id ? '' : item.id)}
        className={`mb-1 flex w-full items-start justify-between gap-2 rounded-lg px-3 py-2 text-left text-sm ${selectedItemId === item.id ? 'bg-emerald-50 text-emerald-900 dark:bg-emerald-500/10 dark:text-emerald-200' : 'hover:bg-slate-50 dark:hover:bg-slate-800'}`}
        style={{ marginLeft: depth * 12, width: `calc(100% - ${depth * 12}px)` }}
      >
        <span><span className="font-medium">{item.title}</span><span className="ml-2 text-xs text-slate-500">{item.kind}</span></span>
        <span className={`shrink-0 text-[10px] ${item.verificationStatus === 'Verified' ? 'text-emerald-700 dark:text-emerald-400' : 'text-amber-700 dark:text-amber-400'}`}>
          {item.verificationStatus === 'Verified' ? 'Verified' : 'Unverified'}
        </span>
      </button>
      {item.children.length > 0 && <TopicList items={item.children} selectedItemId={selectedItemId} onSelect={onSelect} depth={depth + 1} />}
    </div>
  ))
}

function toLocalDate(date: Date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}

export function WrittenPage() {
  const { accessToken } = useAuth()
  const [versions, setVersions] = useState<SyllabusVersion[]>([])
  const [versionId, setVersionId] = useState('')
  const [tree, setTree] = useState<SyllabusTree | null>(null)
  const [questions, setQuestions] = useState<WrittenQuestion[]>([])
  const [questionPage, setQuestionPage] = useState(1)
  const [questionTotalPages, setQuestionTotalPages] = useState(1)
  const [answers, setAnswers] = useState<WrittenAnswerListItem[]>([])
  const [goals, setGoals] = useState<WrittenGoal[]>([])
  const [progress, setProgress] = useState<{ submittedCount: number; submittedWords: number; writingTimeSeconds: number } | null>(null)
  const [subjectId, setSubjectId] = useState('')
  const [itemId, setItemId] = useState('')
  const [type, setType] = useState<WrittenQuestionType | ''>('')
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [treeLoading, setTreeLoading] = useState(false)
  const [questionLoading, setQuestionLoading] = useState(false)
  const [error, setError] = useState('')
  const [goalBusy, setGoalBusy] = useState(false)
  const [goalMessage, setGoalMessage] = useState('')
  const [targetSubmissions, setTargetSubmissions] = useState(3)
  const [targetWords, setTargetWords] = useState(1500)

  useEffect(() => {
    let active = true
    syllabusApi.getVersions({ stageCode: 'Written', pageSize: 50, sortBy: 'stage' })
      .then((result) => {
        if (!active) return
        setVersions(result.items)
        setVersionId(result.items[0]?.id ?? '')
      })
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load Written syllabus versions.'))
      .finally(() => active && setLoading(false))
    return () => { active = false }
  }, [])

  useEffect(() => {
    if (!versionId) {
      setTree(null)
      return
    }
    let active = true
    setTreeLoading(true)
    syllabusApi.getTree(versionId)
      .then((result) => active && setTree(result))
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load the Written syllabus tree.'))
      .finally(() => active && setTreeLoading(false))
    return () => { active = false }
  }, [versionId])

  useEffect(() => {
    let active = true
    setQuestionLoading(true)
    writtenApi.getQuestions({
      search: search || undefined,
      syllabusVersionId: versionId || undefined,
      subjectId: subjectId || undefined,
      syllabusItemId: itemId || undefined,
      type: type || undefined,
      pageNumber: questionPage,
      pageSize: 20,
    }, accessToken ?? undefined)
      .then((result) => {
        if (!active) return
        setQuestions(result.items)
        setQuestionTotalPages(result.totalPages)
      })
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load Written questions.'))
      .finally(() => active && setQuestionLoading(false))
    return () => { active = false }
  }, [accessToken, itemId, questionPage, search, subjectId, type, versionId])

  useEffect(() => {
    if (!accessToken) {
      setAnswers([])
      setGoals([])
      setProgress(null)
      return
    }
    let active = true
    Promise.all([
      writtenApi.getAnswers({ pageNumber: 1, pageSize: 5 }, accessToken),
      writtenApi.getGoals(accessToken),
      writtenApi.getProgress({}, accessToken),
    ])
      .then(([answerResult, goalResult, progressResult]) => {
        if (!active) return
        setAnswers(answerResult.items)
        setGoals(goalResult)
        setProgress(progressResult)
      })
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load your Written progress.'))
    return () => { active = false }
  }, [accessToken])

  const noTopics = useMemo(
    () => tree !== null && tree.subjects.every((subject) => subject.items.length === 0),
    [tree],
  )

  async function createGoal() {
    if (!accessToken) return
    setGoalBusy(true)
    setGoalMessage('')
    const startDate = new Date()
    const endDate = new Date()
    endDate.setDate(endDate.getDate() + 6)
    try {
      const goal = await writtenApi.createGoal({
        startDate: toLocalDate(startDate),
        endDate: toLocalDate(endDate),
        targetSubmissions,
        targetWords,
      }, accessToken)
      setGoals((current) => [goal, ...current])
      setGoalMessage('A seven-day writing goal was created.')
    } catch (reason) {
      setGoalMessage(reason instanceof Error ? reason.message : 'Unable to create a writing goal.')
    } finally {
      setGoalBusy(false)
    }
  }

  return (
    <>
      <PageHeader
        title="Written preparation"
        description="Explore the published Written syllabus, browse sourced questions, and keep private answer drafts and self-assessments."
        action={<div className="flex flex-wrap gap-2"><Link to="/written/answers"><Button variant="secondary"><FileText className="h-4 w-4" />My answers</Button></Link><Link to="/written/bookmarks"><Button variant="secondary"><Bookmark className="h-4 w-4" />Bookmarks</Button></Link><Link to="/written/notes"><Button variant="secondary"><FileText className="h-4 w-4" />Study notes</Button></Link></div>}
      />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      <div className="grid gap-5 xl:grid-cols-[minmax(0,0.9fr)_minmax(0,1.5fr)]">
        <div className="space-y-4">
          {accessToken && (
            <Card className="p-5">
              <CardHeader title="Writing progress" subtitle="Calculated from your submitted answers; self-assessment is personal, not an official score." />
              <div className="grid grid-cols-3 gap-3 pt-4 text-center">
                <div><p className="text-xl font-semibold">{progress?.submittedCount ?? 0}</p><p className="text-xs text-slate-500">submitted</p></div>
                <div><p className="text-xl font-semibold">{progress?.submittedWords ?? 0}</p><p className="text-xs text-slate-500">words</p></div>
                <div><p className="text-xl font-semibold">{Math.round((progress?.writingTimeSeconds ?? 0) / 60)}m</p><p className="text-xs text-slate-500">writing time</p></div>
              </div>
              <div className="mt-4 border-t border-slate-100 pt-4 dark:border-slate-800">
                <h3 className="text-sm font-semibold">Writing goals</h3>
                <div className="mt-2 grid grid-cols-2 gap-2">
                  <label className="text-[11px] text-slate-500">Target submissions<input type="number" min="1" max="100" value={targetSubmissions} onChange={(event) => setTargetSubmissions(Number(event.target.value))} className="mt-1 h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-950 dark:text-white" /></label>
                  <label className="text-[11px] text-slate-500">Target words<input type="number" min="1" max="1000000" value={targetWords} onChange={(event) => setTargetWords(Number(event.target.value))} className="mt-1 h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-950 dark:text-white" /></label>
                </div>
                <Button className="mt-2" size="sm" disabled={goalBusy || targetSubmissions < 1 || targetWords < 1} onClick={() => void createGoal()}><Target className="h-3.5 w-3.5" />{goalBusy ? 'Saving...' : 'Create 7-day goal'}</Button>
                {goalMessage && <p className="mt-2 text-xs text-slate-500" role="status">{goalMessage}</p>}
                {goals.slice(0, 3).map((goal) => <p key={goal.id} className="mt-2 text-xs text-slate-600 dark:text-slate-300">{goal.completedSubmissions}/{goal.targetSubmissions} answers · {goal.completedWords}/{goal.targetWords} words · {goal.startDate}–{goal.endDate}</p>)}
              </div>
            </Card>
          )}
          <Card className="p-5">
            <CardHeader title="Written syllabus" subtitle="Version and topic verification status are shown from the official-source workflow." />
            {loading ? <LoadingState label="Loading Written syllabus..." /> : versions.length === 0 ? (
              <EmptyState title="No published Written syllabus version" description="An administrator must publish a Written syllabus version before it can be explored here." />
            ) : (
              <div className="pt-4">
                <label className="mb-3 block text-xs font-medium text-slate-500" htmlFor="written-version">Syllabus version</label>
                <select id="written-version" value={versionId} onChange={(event) => { setVersionId(event.target.value); setSubjectId(''); setItemId(''); setQuestionPage(1) }} className="mb-3 h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-950">
                  {versions.map((version) => <option key={version.id} value={version.id}>{version.versionLabel} · {version.trackCode}</option>)}
                </select>
                {tree && <p className="mb-3 rounded-lg bg-amber-50 px-3 py-2 text-xs leading-5 text-amber-800 dark:bg-amber-950/30 dark:text-amber-300">{tree.version.coverage === 'OutlineOnly' ? 'This version contains only the verified syllabus outline. Detailed topics are not presented as official until source-verified.' : tree.version.summary}</p>}
                {treeLoading ? <LoadingState label="Loading syllabus topics..." /> : noTopics ? (
                  <EmptyState title="No verified topic tree yet" description="Detailed Written PDF topics have not been transcribed and verified. No topic content is invented here." />
                ) : tree ? (
                  <div className="max-h-[34rem] space-y-3 overflow-auto">
                    <button type="button" onClick={() => { setSubjectId(''); setItemId(''); setQuestionPage(1) }} className={`w-full rounded-lg px-3 py-2 text-left text-sm font-medium ${!subjectId && !itemId ? 'bg-emerald-50 text-emerald-800 dark:bg-emerald-500/10' : 'hover:bg-slate-50 dark:hover:bg-slate-800'}`}>All subjects</button>
                    {tree.subjects.map((subject) => <section key={subject.id} className="rounded-lg border border-slate-100 p-2 dark:border-slate-800">
                      <button type="button" onClick={() => { setSubjectId(subject.subjectId); setItemId(''); setQuestionPage(1) }} className={`w-full rounded-md px-2 py-2 text-left text-sm font-semibold ${subjectId === subject.subjectId ? 'text-emerald-700 dark:text-emerald-400' : ''}`}>{subject.subjectName}{subject.marks === null ? '' : ` · ${subject.marks} marks`}</button>
                      <TopicList items={subject.items} selectedItemId={itemId} onSelect={(id) => { setSubjectId(subject.subjectId); setItemId(id); setQuestionPage(1) }} />
                    </section>)}
                  </div>
                ) : null}
              </div>
            )}
          </Card>
        </div>
        <div className="space-y-4">
          {accessToken && answers.length > 0 && (
            <Card>
              <CardHeader title="Continue writing" action={<Link to="/written/answers" className="text-sm text-emerald-700 hover:underline">View all</Link>} />
              <div className="divide-y divide-slate-100 dark:divide-slate-800">
                {answers.map((answer) => <Link key={answer.id} to={`/written/answers/${answer.id}`} className="block p-4 hover:bg-slate-50 dark:hover:bg-slate-800/60">
                  <div className="flex items-center justify-between gap-2"><span className="line-clamp-2 text-sm font-medium">{answer.questionPrompt}</span><Badge>{answer.status}</Badge></div>
                  <p className="mt-1 text-xs text-slate-500">{answer.wordCount} words · {answer.topicTitle}</p>
                </Link>)}
              </div>
            </Card>
          )}
          <Card className="p-5">
            <CardHeader title="Written question bank" subtitle="Browse and filter source-attributed questions by type and syllabus mapping." />
            <form className="grid gap-3 pt-4 sm:grid-cols-[minmax(0,1fr)_12rem_auto]" onSubmit={(event) => { event.preventDefault(); setSearch(searchInput.trim()); setQuestionPage(1) }}>
              <input aria-label="Search Written questions" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} placeholder="Search questions" className="h-10 rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-950" />
              <select aria-label="Question type" value={type} onChange={(event) => { setType(event.target.value as WrittenQuestionType | ''); setQuestionPage(1) }} className="h-10 rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-950">
                <option value="">All types</option>{questionTypes.map((option) => <option key={option} value={option}>{option}</option>)}
              </select>
              <Button type="submit"><BookOpen className="h-4 w-4" />Search</Button>
            </form>
            {questionLoading ? <LoadingState label="Loading Written questions..." /> : questions.length === 0 ? (
              <EmptyState title="No published Written questions" description={noTopics ? 'Questions can be published after verified Written topics and question-source references have been added by an authorized editor.' : 'Try another filter or check back after source-attributed questions are published.'} />
            ) : (
              <div className="mt-4 space-y-3">
                {questions.map((question) => <QuestionCard key={question.id} question={question} />)}
                {questionTotalPages > 1 && <Pagination page={questionPage} totalPages={questionTotalPages} onPageChange={setQuestionPage} />}
              </div>
            )}
          </Card>
        </div>
      </div>
    </>
  )
}

function QuestionCard({ question }: { question: WrittenQuestion }) {
  return (
    <article className="rounded-lg border border-slate-200 p-4 dark:border-slate-700">
      <div className="flex flex-wrap items-center gap-2"><Badge>{question.type}</Badge>{question.bcsExamYear && <Badge>{question.bcsExamYear} BCS</Badge>}{question.marks !== null && <Badge>{question.marks} marks</Badge>}</div>
      <h3 className="mt-3 line-clamp-3 text-sm font-medium leading-6">{question.prompt}</h3>
      <p className="mt-2 text-xs text-slate-500">{question.subjectName}{question.paperName ? ` · ${question.paperName}` : ''} · {question.topicTitle}{question.parentTopicTitle ? ` / ${question.parentTopicTitle}` : ''} · {question.syllabusVersionLabel}</p>
      <p className="mt-2 text-xs text-slate-500">{question.sources.length} cited source{question.sources.length === 1 ? '' : 's'}{question.hasModelAnswer ? ' · model answer available' : ''}</p>
      <Link to={`/written/questions/${question.id}`} className="mt-3 inline-block text-sm font-medium text-emerald-700 hover:underline dark:text-emerald-400">Open question</Link>
    </article>
  )
}

export function WrittenAnswersPage({ bookmarks = false }: { bookmarks?: boolean }) {
  const { accessToken } = useAuth()
  const [answers, setAnswers] = useState<WrittenAnswerListItem[]>([])
  const [savedQuestions, setSavedQuestions] = useState<Array<{ questionId: string; prompt: string; type: WrittenQuestionType; topicTitle: string }>>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    if (!accessToken) {
      setLoading(false)
      return () => { active = false }
    }
    const request = bookmarks
      ? writtenApi.getBookmarks({ pageNumber: 1, pageSize: 50 }, accessToken).then((result) => active && setSavedQuestions(result.items))
      : writtenApi.getAnswers({ pageNumber: 1, pageSize: 50 }, accessToken).then((result) => active && setAnswers(result.items))
    request.catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load Written records.'))
      .finally(() => active && setLoading(false))
    return () => { active = false }
  }, [accessToken, bookmarks])

  return (
    <>
      <PageHeader title={bookmarks ? 'Bookmarked Written questions' : 'My Written answers'} description={bookmarks ? 'Questions saved privately to your account.' : 'Drafts, submissions, and revision history for your answer-writing practice.'} />
      {!accessToken ? <Card><EmptyState title="Sign in to continue" description="Your Written answers and bookmarks are private to your account." /></Card>
        : error ? <ErrorState description={error} />
          : loading ? <LoadingState label="Loading Written records..." />
            : bookmarks ? savedQuestions.length === 0 ? <Card><EmptyState title="No bookmarks yet" description="Bookmark a question from its detail page to keep it here." /></Card>
              : <div className="space-y-3">{savedQuestions.map((question) => <Card key={question.questionId} className="p-4"><Badge>{question.type}</Badge><h2 className="mt-2 font-medium">{question.prompt}</h2><p className="mt-1 text-xs text-slate-500">{question.topicTitle}</p><Link className="mt-3 inline-block text-sm text-emerald-700 hover:underline" to={`/written/questions/${question.questionId}`}>Open question</Link></Card>)}</div>
              : answers.length === 0 ? <Card><EmptyState title="No answers yet" description="Choose a published Written question and create a private draft to begin." /></Card>
                : <div className="space-y-3">{answers.map((answer) => <Card key={answer.id} className="p-4"><div className="flex flex-wrap items-start justify-between gap-2"><div><Badge>{answer.status}</Badge><h2 className="mt-2 font-medium">{answer.questionPrompt}</h2><p className="mt-1 text-xs text-slate-500">{answer.topicTitle} · {answer.wordCount} words · Updated {new Date(answer.updatedAt).toLocaleString()}</p></div><Link className="text-sm font-medium text-emerald-700 hover:underline" to={`/written/answers/${answer.id}`}>{answer.status === 'Submitted' ? 'Review / revise' : 'Continue draft'}</Link></div></Card>)}</div>}
    </>
  )
}
