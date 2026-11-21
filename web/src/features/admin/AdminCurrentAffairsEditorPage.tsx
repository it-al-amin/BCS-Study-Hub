import { Archive, ArrowLeft, Eye, Plus, Save, Search, Send, Trash2, X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { ErrorState, LoadingState } from '../../components/ui/States'
import {
  currentAffairsApi,
  currentAffairsCategories,
  type CurrentAffairsCategory,
  type CurrentAffairsDetail,
  type CurrentAffairsEntryInput,
  type OfficialSource,
  type QuestionType,
  type ReferenceKind,
} from '../../services/currentAffairs'
import { categoryLabels, errorMessage } from '../current-affairs/currentAffairsFormat'
import { SyllabusMappingPicker, type PickedRef } from '../library/libraryUi'
import { MarkdownEditor } from '../../components/content/MarkdownEditor'
import { ResourcePicker } from '../../components/content/ResourcePicker'
import { AuditHistory } from '../../components/content/AuditHistory'

const fieldClass = 'mt-1 h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-950'
const labelClass = 'block text-xs font-medium text-slate-600 dark:text-slate-300'

interface FormState {
  title: string
  summary: string
  content: string
  eventDate: string
  sourcePublishedOn: string
  category: CurrentAffairsCategory
  sourceName: string
  sourceUrl: string
  officialSourceId: string
  sourceVerified: boolean
  notes: string
  tags: string
  subjects: PickedRef[]
  topics: PickedRef[]
  questions: { type: QuestionType; id: string; label: string }[]
  references: { kind: ReferenceKind; title: string; url: string }[]
  resources: PickedRef[]
}

function todayIso() {
  const now = new Date(Date.now() + 6 * 60 * 60 * 1000)
  return now.toISOString().slice(0, 10)
}

const emptyForm = (): FormState => ({
  title: '', summary: '', content: '', eventDate: todayIso(), sourcePublishedOn: '', category: 'Bangladesh', sourceName: '', sourceUrl: '',
  officialSourceId: '', sourceVerified: false, notes: '', tags: '', subjects: [], topics: [], questions: [], references: [], resources: [],
})

function fromDetail(detail: CurrentAffairsDetail): FormState {
  const entry = detail.entry
  return {
    title: entry.title,
    summary: entry.summary,
    content: detail.content ?? '',
    eventDate: entry.eventDate,
    sourcePublishedOn: entry.sourcePublishedOn ?? '',
    category: entry.category,
    sourceName: entry.sourceName,
    sourceUrl: entry.sourceUrl,
    officialSourceId: detail.officialSource?.id ?? '',
    sourceVerified: entry.sourceStatus === 'Verified',
    notes: detail.notes ?? '',
    tags: entry.tags.join(', '),
    subjects: entry.subjects.map((subject) => ({ id: subject.id, label: subject.name })),
    topics: entry.topics.map((topic) => ({ id: topic.id, label: `${topic.title} (${topic.subjectName} · ${topic.stageCode} ${topic.versionLabel})` })),
    questions: detail.questions.map((question) => ({ type: question.type, id: question.id, label: `${question.prompt}${question.isAvailable ? '' : ` [${question.status}]`}` })),
    references: detail.references.map((reference) => ({ kind: reference.kind, title: reference.title, url: reference.url })),
    resources: detail.libraryResources.map((resource) => ({ id: resource.id, label: `${resource.title}${resource.isAvailable ? '' : ' [no longer shared]'}` })),
  }
}

function toInput(form: FormState): CurrentAffairsEntryInput {
  const text = (value: string) => value.trim() || null
  return {
    title: form.title.trim(),
    summary: form.summary.trim(),
    content: text(form.content),
    eventDate: form.eventDate,
    sourcePublishedOn: text(form.sourcePublishedOn),
    category: form.category,
    sourceName: form.sourceName.trim(),
    sourceUrl: form.sourceUrl.trim(),
    officialSourceId: form.officialSourceId || null,
    sourceStatus: form.sourceVerified ? 'Verified' : 'Unverified',
    notes: text(form.notes),
    tags: [...new Map(form.tags.split(',').map((tag) => tag.trim()).filter(Boolean).map((tag) => [tag.toLocaleLowerCase(), tag])).values()],
    subjectIds: form.subjects.map((subject) => subject.id),
    syllabusItemIds: form.topics.map((topic) => topic.id),
    questions: form.questions.map((question) => ({ type: question.type, questionId: question.id })),
    references: form.references.filter((reference) => reference.title.trim() || reference.url.trim()),
    libraryResourceIds: form.resources.map((resource) => resource.id),
  }
}

export function AdminCurrentAffairsEditorPage() {
  const { entryId } = useParams()
  return <Editor key={entryId ?? 'new'} entryId={entryId ?? null} />
}

function Editor({ entryId }: { entryId: string | null }) {
  const { accessToken } = useAuth()
  const navigate = useNavigate()
  const [detail, setDetail] = useState<CurrentAffairsDetail | null>(null)
  const [form, setForm] = useState<FormState>(emptyForm)
  const [sources, setSources] = useState<OfficialSource[]>([])
  const [loading, setLoading] = useState(Boolean(entryId))
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  useEffect(() => {
    if (!accessToken) return
    let active = true
    currentAffairsApi.getAdminSources({}, accessToken).then((result) => active && setSources(result.items)).catch(() => undefined)
    if (entryId) {
      currentAffairsApi.getAdminEntry(entryId, accessToken)
        .then((result) => {
          if (!active) return
          setDetail(result)
          setForm(fromDetail(result))
        })
        .catch((reason: unknown) => active && setError(errorMessage(reason, 'Unable to load the entry.')))
        .finally(() => active && setLoading(false))
    }
    return () => { active = false }
  }, [accessToken, entryId])

  function set<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm((current) => ({ ...current, [key]: value }))
  }

  async function save() {
    if (!accessToken) return
    setSaving(true)
    setError('')
    setMessage('')
    try {
      if (detail) {
        const updated = await currentAffairsApi.updateEntry(detail.entry.id, toInput(form), accessToken)
        setDetail(updated)
        setForm(fromDetail(updated))
        setMessage('Saved.')
      } else {
        const created = await currentAffairsApi.createEntry(toInput(form), accessToken)
        navigate(`/admin/current-affairs/${created.entry.id}/edit`, { replace: true })
      }
    } catch (reason) {
      setError(errorMessage(reason, 'Unable to save the entry.'))
    } finally {
      setSaving(false)
    }
  }

  async function changeStatus(action: 'publish' | 'archive') {
    if (!accessToken || !detail) return
    setError('')
    try {
      const updated = await currentAffairsApi.setEntryStatus(detail.entry.id, action, accessToken)
      setDetail(updated)
      setMessage(action === 'publish' ? 'Published.' : 'Archived.')
    } catch (reason) {
      setError(errorMessage(reason, 'Status change failed.'))
    }
  }

  async function remove() {
    if (!accessToken || !detail || !window.confirm('Delete this entry? Learner bookmarks, notes and revision records for it are removed too.')) return
    try {
      await currentAffairsApi.deleteEntry(detail.entry.id, accessToken)
      navigate('/admin/current-affairs')
    } catch (reason) {
      setError(errorMessage(reason, 'Unable to delete the entry.'))
    }
  }

  if (!accessToken) return null
  if (loading) return <LoadingState label="Loading entry..." />
  const status = detail?.entry.status

  return (
    <>
      <Link to="/admin/current-affairs" className="mb-4 inline-flex items-center gap-1 text-sm text-slate-500 hover:text-slate-800 dark:hover:text-slate-200"><ArrowLeft className="h-4 w-4" />All entries</Link>
      <PageHeader
        title={detail ? 'Edit current-affairs entry' : 'New current-affairs entry'}
        description="Write the entry yourself from the cited source. Summarize in your own words and link to the original instead of copying text."
        action={detail && <div className="flex flex-wrap items-center gap-2">
          <Badge tone={status === 'Published' ? 'success' : status === 'Draft' ? 'warning' : 'neutral'}>{status}</Badge>
          {status !== 'Published' && <Button size="sm" onClick={() => void changeStatus('publish')}><Send className="h-4 w-4" />Publish</Button>}
          {status === 'Published' && <Link to={`/current-affairs/${detail.entry.id}`}><Button size="sm" variant="secondary"><Eye className="h-4 w-4" />View</Button></Link>}
          {status !== 'Archived' && <Button size="sm" variant="secondary" onClick={() => void changeStatus('archive')}><Archive className="h-4 w-4" />Archive</Button>}
          {status !== 'Published' && <Button size="sm" variant="ghost" onClick={() => void remove()}><Trash2 className="h-4 w-4" />Delete</Button>}
        </div>}
      />
      {error && <div className="mb-4"><ErrorState title="Not saved" description={error} /></div>}
      {message && <p className="mb-4 rounded-lg bg-emerald-50 px-4 py-3 text-sm text-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-300" role="status">{message}</p>}
      <form className="grid gap-5 xl:grid-cols-[minmax(0,1.3fr)_minmax(0,1fr)]" onSubmit={(event) => { event.preventDefault(); void save() }}>
        <Card className="space-y-4 p-5">
          <label className={labelClass}>Title<input required maxLength={300} className={fieldClass} value={form.title} onChange={(event) => set('title', event.target.value)} /></label>
          <div className="grid gap-3 sm:grid-cols-3">
            <label className={labelClass}>Category<select className={fieldClass} value={form.category} onChange={(event) => set('category', event.target.value as CurrentAffairsCategory)}>{currentAffairsCategories.map((value) => <option key={value} value={value}>{categoryLabels[value]}</option>)}</select></label>
            <label className={labelClass}>Event date<input type="date" required className={fieldClass} value={form.eventDate} onChange={(event) => set('eventDate', event.target.value)} /></label>
            <label className={labelClass}>Source published on<input type="date" className={fieldClass} value={form.sourcePublishedOn} onChange={(event) => set('sourcePublishedOn', event.target.value)} /></label>
          </div>
          <label className={labelClass}>Summary (shown in lists and printed collections)<textarea required maxLength={2000} rows={3} className="mt-1 w-full rounded-lg border border-slate-200 bg-white p-3 text-sm dark:border-slate-700 dark:bg-slate-950" value={form.summary} onChange={(event) => set('summary', event.target.value)} /></label>
          <MarkdownEditor label="Detailed content" maxLength={100000} rows={12} value={form.content} onChange={(value) => set('content', value)} />
          <label className={labelClass}>Editor's note (exam relevance, caveats)<textarea maxLength={4000} rows={2} className="mt-1 w-full rounded-lg border border-slate-200 bg-white p-3 text-sm dark:border-slate-700 dark:bg-slate-950" value={form.notes} onChange={(event) => set('notes', event.target.value)} /></label>
          <label className={labelClass}>Tags (comma separated)<input className={fieldClass} value={form.tags} onChange={(event) => set('tags', event.target.value)} placeholder="budget, FY2026-27" /></label>
          <div><p className={labelClass}>Related subjects and syllabus topics</p><div className="mt-1"><SyllabusMappingPicker subjects={form.subjects} topics={form.topics} onSubjectsChange={(value) => set('subjects', value)} onTopicsChange={(value) => set('topics', value)} /></div></div>
        </Card>
        <div className="space-y-5">
          <Card className="space-y-3 p-5">
            <h2 className="font-semibold">Source</h2>
            <label className={labelClass}>Source name<input required maxLength={200} className={fieldClass} value={form.sourceName} onChange={(event) => set('sourceName', event.target.value)} placeholder="e.g. Finance Division" /></label>
            <label className={labelClass}>Source URL<input type="url" required maxLength={1024} className={fieldClass} value={form.sourceUrl} onChange={(event) => set('sourceUrl', event.target.value)} placeholder="https://" /></label>
            <label className={labelClass}>Directory organization (optional)<select className={fieldClass} value={form.officialSourceId} onChange={(event) => {
              const chosen = sources.find((source) => source.id === event.target.value)
              setForm((current) => ({ ...current, officialSourceId: event.target.value, sourceName: current.sourceName || chosen?.name || '' }))
            }}><option value="">—</option>{sources.map((source) => <option key={source.id} value={source.id}>{source.name}{source.status === 'Published' ? '' : ` (${source.status})`}</option>)}</select></label>
            <label className="flex items-start gap-2 rounded-lg border border-slate-200 p-3 text-sm dark:border-slate-700"><input type="checkbox" className="mt-1" checked={form.sourceVerified} onChange={(event) => set('sourceVerified', event.target.checked)} /><span>I opened the source URL and confirmed it supports this entry. <span className="block text-xs text-slate-500">Required before publishing.</span></span></label>
          </Card>
          <QuestionPicker token={accessToken} value={form.questions} onChange={(value) => set('questions', value)} />
          <Card className="space-y-3 p-5">
            <CardHeader title="PDF, image and web references" subtitle="Link to files hosted by the publisher." action={<Button type="button" size="sm" variant="secondary" disabled={form.references.length >= 20} onClick={() => set('references', [...form.references, { kind: 'Pdf', title: '', url: '' }])}><Plus className="h-4 w-4" />Add</Button>} />
            {form.references.map((reference, index) => <div key={index} className="grid grid-cols-[5.5rem_1fr_auto] gap-2">
              <select aria-label="Reference kind" className="h-9 rounded-lg border border-slate-200 bg-white px-1 text-sm dark:border-slate-700 dark:bg-slate-950" value={reference.kind} onChange={(event) => set('references', form.references.map((item, position) => position === index ? { ...item, kind: event.target.value as ReferenceKind } : item))}><option value="Pdf">PDF</option><option value="Image">Image</option><option value="WebPage">Web page</option></select>
              <div className="space-y-1"><input aria-label="Reference title" placeholder="Title" maxLength={300} className="h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950" value={reference.title} onChange={(event) => set('references', form.references.map((item, position) => position === index ? { ...item, title: event.target.value } : item))} /><input aria-label="Reference URL" type="url" placeholder="https://" maxLength={1024} className="h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950" value={reference.url} onChange={(event) => set('references', form.references.map((item, position) => position === index ? { ...item, url: event.target.value } : item))} /></div>
              <button type="button" aria-label="Remove reference" className="self-start p-2 text-slate-400 hover:text-red-600" onClick={() => set('references', form.references.filter((_, position) => position !== index))}><X className="h-4 w-4" /></button>
            </div>)}
          </Card>
          <Card className="p-5"><ResourcePicker title="Library files" description="Attach shared, published library PDFs or images; their rights were checked on upload." value={form.resources} onChange={(value) => set('resources', value)} search={async (term) => (await currentAffairsApi.searchResources(term, accessToken)).map((item) => ({ id: item.id, title: item.title, detail: item.primaryFormat ?? item.kind }))} /></Card>
          {detail && <AuditHistory entityType="CurrentAffairsEntry" entityId={detail.entry.id} />}
          <div className="flex justify-end"><Button type="submit" disabled={saving || !form.title.trim() || !form.summary.trim()}><Save className="h-4 w-4" />{saving ? 'Saving...' : detail ? 'Save changes' : 'Create draft'}</Button></div>
        </div>
      </form>
    </>
  )
}

function QuestionPicker({ token, value, onChange }: { token: string; value: FormState['questions']; onChange: (value: FormState['questions']) => void }) {
  const [type, setType] = useState<QuestionType | ''>('')
  const [search, setSearch] = useState('')
  const [results, setResults] = useState<{ type: QuestionType; id: string; prompt: string; status: string }[]>([])
  const [error, setError] = useState('')

  async function find() {
    setError('')
    try {
      setResults(await currentAffairsApi.searchQuestions(type, search.trim(), token))
    } catch (reason) {
      setError(errorMessage(reason, 'Search failed.'))
    }
  }

  return (
    <Card className="space-y-3 p-5">
      <h2 className="font-semibold">Related questions</h2>
      <ul className="space-y-1">{value.map((question) => <li key={`${question.type}-${question.id}`} className="flex items-start gap-2 text-sm"><Badge tone="neutral">{question.type}</Badge><span className="min-w-0 flex-1">{question.label}</span><button type="button" aria-label="Remove question" className="text-slate-400 hover:text-red-600" onClick={() => onChange(value.filter((item) => !(item.type === question.type && item.id === question.id)))}><X className="h-4 w-4" /></button></li>)}</ul>
      <div className="flex gap-2"><select aria-label="Question type" className="h-9 rounded-lg border border-slate-200 bg-white px-1 text-sm dark:border-slate-700 dark:bg-slate-950" value={type} onChange={(event) => setType(event.target.value as QuestionType | '')}><option value="">All banks</option><option value="Mcq">MCQ</option><option value="Written">Written</option><option value="Viva">Viva</option></select><input aria-label="Search questions" placeholder="Search question text" className="h-9 min-w-0 flex-1 rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950" value={search} onChange={(event) => setSearch(event.target.value)} onKeyDown={(event) => { if (event.key === 'Enter') { event.preventDefault(); void find() } }} /><Button type="button" size="sm" variant="secondary" disabled={search.trim().length < 2} onClick={() => void find()}><Search className="h-4 w-4" /></Button></div>
      {error && <p className="text-xs text-red-600">{error}</p>}
      {results.length > 0 && <ul className="max-h-48 space-y-1 overflow-auto">{results.map((result) => {
        const added = value.some((item) => item.type === result.type && item.id === result.id)
        return <li key={`${result.type}-${result.id}`}><button type="button" disabled={added || value.length >= 30} onClick={() => onChange([...value, { type: result.type, id: result.id, label: result.prompt }])} className="w-full rounded px-2 py-1 text-left text-sm hover:bg-slate-50 disabled:opacity-50 dark:hover:bg-slate-800"><Badge tone="neutral">{result.type}</Badge> {result.prompt} <span className="text-xs text-slate-500">({result.status})</span></button></li>
      })}</ul>}
    </Card>
  )
}

