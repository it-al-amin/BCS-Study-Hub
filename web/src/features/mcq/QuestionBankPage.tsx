import { Bookmark, BookmarkCheck, ExternalLink, Search } from 'lucide-react'
import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { Field, Input, Select } from '../../components/ui/Form'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { mcqApi, type McqQuestion, type McqQuestionQuery } from '../../services/mcq'
import { syllabusApi, type SyllabusItem, type SyllabusTree, type SyllabusVersion } from '../../services/syllabus'

function flattenItems(items: SyllabusItem[]): SyllabusItem[] {
  return items.flatMap((item) => [item, ...flattenItems(item.children)])
}

export function QuestionBankPage() {
  const { accessToken } = useAuth()
  const [versions, setVersions] = useState<SyllabusVersion[]>([])
  const [versionId, setVersionId] = useState('')
  const [tree, setTree] = useState<SyllabusTree | null>(null)
  const [questions, setQuestions] = useState<McqQuestion[]>([])
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [form, setForm] = useState({
    search: '',
    subjectId: '',
    syllabusItemId: '',
    year: '',
    difficulty: '',
    tag: '',
  })
  const [filters, setFilters] = useState<McqQuestionQuery>({ sortBy: 'recent' })
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [savingBookmark, setSavingBookmark] = useState<Set<string>>(new Set())
  const [bookmarkError, setBookmarkError] = useState('')

  useEffect(() => {
    let active = true
    syllabusApi.getVersions({ stageCode: 'Preliminary', pageSize: 100 })
      .then((result) => {
        if (active) {
          setVersions(result.items)
          setVersionId(result.items[0]?.id ?? '')
        }
      })
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load Preliminary syllabus versions.'))
    return () => { active = false }
  }, [])

  useEffect(() => {
    let active = true
    if (!versionId) {
      setTree(null)
      return () => { active = false }
    }

    syllabusApi.getTree(versionId)
      .then((result) => active && setTree(result))
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load syllabus filters.'))
    return () => { active = false }
  }, [versionId])

  const loadQuestions = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const result = await mcqApi.getQuestions({ ...filters, syllabusVersionId: versionId || undefined, pageNumber: page, pageSize: 12 }, accessToken ?? undefined)
      setQuestions(result.items)
      setTotalPages(Math.max(result.totalPages, 1))
    } catch (reason) {
      setQuestions([])
      setError(reason instanceof Error ? reason.message : 'Unable to load questions.')
    } finally {
      setLoading(false)
    }
  }, [accessToken, filters, page, versionId])

  useEffect(() => { void loadQuestions() }, [loadQuestions])

  function submitFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setPage(1)
    setFilters({
      search: form.search.trim() || undefined,
      syllabusVersionId: versionId || undefined,
      subjectId: form.subjectId || undefined,
      syllabusItemId: form.syllabusItemId || undefined,
      bcsExamYear: form.year ? Number(form.year) : undefined,
      difficulty: form.difficulty ? form.difficulty as McqQuestionQuery['difficulty'] : undefined,
      tag: form.tag.trim() || undefined,
      sortBy: 'recent',
    })
  }

  async function toggleBookmark(question: McqQuestion) {
    if (!accessToken) return
    setBookmarkError('')
    setSavingBookmark((current) => new Set(current).add(question.id))
    try {
      if (question.isBookmarked) await mcqApi.removeBookmark(question.id, accessToken)
      else await mcqApi.addBookmark(question.id, accessToken)
      setQuestions((current) => current.map((item) => item.id === question.id
        ? { ...item, isBookmarked: !item.isBookmarked }
        : item))
    } catch (reason) {
      setBookmarkError(reason instanceof Error ? reason.message : 'Unable to update bookmark.')
    } finally {
      setSavingBookmark((current) => {
        const next = new Set(current)
        next.delete(question.id)
        return next
      })
    }
  }

  return (
    <>
      <PageHeader
        title="Preliminary question bank"
        description="Browse source-attributed questions mapped to verified Preliminary topics. Correct answers and explanations are reserved for practice review."
        action={<Link to="/practice" className="rounded-lg bg-emerald-700 px-4 py-2.5 text-sm font-medium text-white hover:bg-emerald-800">Practice sets</Link>}
      />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {bookmarkError && <p className="mb-4 text-sm text-red-600" role="alert">{bookmarkError}</p>}

      <Card className="mb-6 p-5">
        <form onSubmit={submitFilters} className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <Field label="Syllabus version">
            <Select value={versionId} onChange={(event) => {
              setVersionId(event.target.value)
              setForm((current) => ({ ...current, subjectId: '', syllabusItemId: '' }))
              setFilters({ sortBy: 'recent' })
              setPage(1)
            }}>
              {versions.map((version) => <option key={version.id} value={version.id}>{version.versionLabel}</option>)}
            </Select>
          </Field>
          <Field label="Subject">
            <Select value={form.subjectId} onChange={(event) => setForm((current) => ({ ...current, subjectId: event.target.value, syllabusItemId: '' }))}>
              <option value="">All subjects</option>
              {tree?.subjects.map((subject) => <option key={subject.subjectId} value={subject.subjectId}>{subject.subjectName}</option>)}
            </Select>
          </Field>
          <Field label="Topic or subtopic">
            <Select value={form.syllabusItemId} onChange={(event) => setForm((current) => ({ ...current, syllabusItemId: event.target.value }))}>
              <option value="">All topics</option>
              {tree?.subjects.filter((subject) => !form.subjectId || subject.subjectId === form.subjectId).flatMap((subject) =>
                flattenItems(subject.items).map((item) => (
                  <option key={item.id} value={item.id}>{subject.subjectName} · {item.title}</option>
                )))}
            </Select>
          </Field>
          <Field label="Search">
            <Input value={form.search} onChange={(event) => setForm((current) => ({ ...current, search: event.target.value }))} placeholder="Question or exam reference" />
          </Field>
          <Field label="BCS exam year">
            <Input type="number" min={1900} max={2200} value={form.year} onChange={(event) => setForm((current) => ({ ...current, year: event.target.value }))} />
          </Field>
          <Field label="Difficulty">
            <Select value={form.difficulty} onChange={(event) => setForm((current) => ({ ...current, difficulty: event.target.value }))}>
              <option value="">All levels</option>
              <option value="Easy">Easy</option>
              <option value="Medium">Medium</option>
              <option value="Hard">Hard</option>
            </Select>
          </Field>
          <Field label="Tag">
            <Input value={form.tag} onChange={(event) => setForm((current) => ({ ...current, tag: event.target.value }))} placeholder="Optional tag" />
          </Field>
          <div className="flex items-end">
            <Button type="submit" className="w-full"><Search className="h-4 w-4" /> Apply filters</Button>
          </div>
        </form>
      </Card>

      {loading ? <LoadingState label="Loading the question bank..." /> : questions.length === 0 ? (
        <Card><EmptyState title="No published questions yet" description="Questions will appear here after an authorized editor adds source-attributed content and publishes it. No sample questions are presented as official." /></Card>
      ) : (
        <div className="space-y-4">
          {questions.map((question) => (
            <Card key={question.id} className="overflow-hidden">
              <CardHeader
                title={`${question.subjectName} · ${question.topicTitle}`}
                subtitle={[question.parentTopicTitle, question.examNumber ? `${question.examNumber}th BCS` : null, question.bcsExamYear, question.examReference, question.difficulty].filter(Boolean).join(' · ')}
                action={accessToken
                  ? <Button variant="ghost" size="sm" disabled={savingBookmark.has(question.id)} onClick={() => void toggleBookmark(question)} aria-label={question.isBookmarked ? 'Remove bookmark' : 'Bookmark question'}>
                    {question.isBookmarked ? <BookmarkCheck className="h-4 w-4 text-emerald-700" /> : <Bookmark className="h-4 w-4" />}
                  </Button>
                  : <Link to="/login" className="text-xs text-emerald-700 hover:underline">Sign in to bookmark</Link>}
              />
              <div className="space-y-4 p-5">
                <div className="flex flex-wrap gap-2">
                  <Badge>{question.type === 'SingleAnswer' ? 'Single answer' : 'Multiple answer'}</Badge>
                  {question.tags.map((tag) => <Badge key={tag}>{tag}</Badge>)}
                </div>
                <p className="whitespace-pre-wrap text-sm font-medium leading-6">{question.prompt}</p>
                {question.imageUrl && <img src={question.imageUrl} alt="Question illustration" className="max-h-80 rounded-lg border border-slate-200 object-contain dark:border-slate-700" />}
                <ul className="grid gap-2 sm:grid-cols-2">
                  {question.options.map((option, index) => (
                    <li key={option.id} className="rounded-lg border border-slate-200 px-3 py-2 text-sm dark:border-slate-700">
                      <span className="mr-2 font-semibold text-slate-400">{String.fromCharCode(65 + index)}.</span>{option.text}
                    </li>
                  ))}
                </ul>
                <div className="flex flex-wrap gap-x-4 gap-y-2 border-t border-slate-100 pt-3 text-xs text-slate-500 dark:border-slate-800">
                  {question.sources.map((source) => (
                    <a key={source.id} href={source.url} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1 hover:text-emerald-700">
                      {source.publisher}: {source.title}{source.citation ? ` (${source.citation})` : ''}<ExternalLink className="h-3 w-3" />
                    </a>
                  ))}
                </div>
              </div>
            </Card>
          ))}
          <div className="flex items-center justify-between rounded-xl border border-slate-200 bg-white px-5 py-3 dark:border-slate-800 dark:bg-slate-900">
            <span className="text-sm text-slate-500">Page {page} of {totalPages}</span>
            <div className="flex gap-2">
              <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => setPage((current) => current - 1)}>Previous</Button>
              <Button variant="secondary" size="sm" disabled={page >= totalPages} onClick={() => setPage((current) => current + 1)}>Next</Button>
            </div>
          </div>
        </div>
      )}
    </>
  )
}
