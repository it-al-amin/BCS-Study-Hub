import { ArrowLeft, Archive, Eye, Plus, Save, Send, Trash2, Upload } from 'lucide-react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { ErrorState, LoadingState } from '../../components/ui/States'
import {
  libraryApi,
  type LibraryChapter,
  type LibraryFileRole,
  type LibraryMode,
  type LibraryResourceCategory,
  type LibraryResourceDetail,
  type LibraryResourceInput,
  type LibraryResourceKind,
  type LibraryResourceSummary,
  type LibraryRightsBasis,
  type LibrarySourceType,
  type LibraryStorageUsage,
} from '../../services/library'
import { SignInRequired } from './LibraryPage'
import { categoryLabels, errorMessage, formatBytes, kindLabels, rightsLabels, sourceTypeLabels } from './libraryFormat'
import { SyllabusMappingPicker, VisibilityBadge, type PickedRef } from './libraryUi'

const fieldClass = 'mt-1 h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-950'
const labelClass = 'block text-xs font-medium text-slate-600 dark:text-slate-300'
const sharedRights: LibraryRightsBasis[] = ['NotApplicable', 'OwnWork', 'PublicDomain', 'GovernmentPublication', 'OpenLicense', 'PermissionGranted']
const notedRights: LibraryRightsBasis[] = ['GovernmentPublication', 'OpenLicense', 'PermissionGranted']

interface FormState {
  kind: LibraryResourceKind
  category: LibraryResourceCategory
  title: string
  description: string
  authors: string
  sourceName: string
  sourceType: LibrarySourceType
  website: string
  url: string
  publicationDate: string
  rightsBasis: LibraryRightsBasis
  rightsNote: string
  noteContent: string
  publisher: string
  edition: string
  publicationYear: string
  isbn: string
  pageCount: string
  language: string
  tags: string
  subjects: PickedRef[]
  topics: PickedRef[]
}

function emptyForm(kind: LibraryResourceKind, mode: LibraryMode): FormState {
  return {
    kind,
    category: kind === 'Book' ? 'Textbook' : kind === 'Note' ? 'StudyNote' : 'General',
    title: '', description: '', authors: '', sourceName: '', sourceType: 'Unspecified', website: '', url: '', publicationDate: '',
    rightsBasis: kind === 'File' ? (mode === 'admin' ? 'OwnWork' : 'PersonalCopy') : kind === 'Note' ? 'OwnWork' : 'NotApplicable',
    rightsNote: '', noteContent: '', publisher: '', edition: '', publicationYear: '', isbn: '', pageCount: '', language: '', tags: '', subjects: [], topics: [],
  }
}

function fromDetail(detail: LibraryResourceDetail): FormState {
  const resource = detail.resource
  return {
    kind: resource.kind,
    category: resource.category,
    title: resource.title,
    description: resource.description ?? '',
    authors: resource.authors ?? '',
    sourceName: resource.sourceName ?? '',
    sourceType: resource.sourceType,
    website: detail.website ?? '',
    url: resource.url ?? '',
    publicationDate: resource.publicationDate ?? '',
    rightsBasis: detail.rightsBasis,
    rightsNote: detail.rightsNote ?? '',
    noteContent: detail.noteContent ?? '',
    publisher: resource.book?.publisher ?? '',
    edition: resource.book?.edition ?? '',
    publicationYear: resource.book?.publicationYear?.toString() ?? '',
    isbn: resource.book?.isbn ?? '',
    pageCount: resource.book?.pageCount?.toString() ?? '',
    language: resource.book?.language ?? '',
    tags: resource.tags.join(', '),
    subjects: resource.subjects.map((subject) => ({ id: subject.id, label: subject.name })),
    topics: resource.topics.map((topic) => ({ id: topic.id, label: `${topic.title} (${topic.subjectName} · ${topic.stageCode} ${topic.versionLabel})` })),
  }
}

const text = (value: string) => value.trim() || null
const integer = (value: string) => (value.trim() ? Number.parseInt(value, 10) : null)

function toInput(form: FormState): LibraryResourceInput {
  return {
    kind: form.kind,
    category: form.category,
    title: form.title.trim(),
    description: text(form.description),
    authors: text(form.authors),
    sourceName: text(form.sourceName),
    sourceType: form.sourceType,
    website: text(form.website),
    url: text(form.url),
    publicationDate: text(form.publicationDate),
    rightsBasis: form.rightsBasis,
    rightsNote: text(form.rightsNote),
    noteContent: text(form.noteContent),
    book: form.kind === 'Book'
      ? { publisher: text(form.publisher), edition: text(form.edition), publicationYear: integer(form.publicationYear), isbn: text(form.isbn), pageCount: integer(form.pageCount), language: text(form.language) }
      : null,
    subjectIds: form.subjects.map((subject) => subject.id),
    syllabusItemIds: form.topics.map((topic) => topic.id),
    tags: [...new Map(form.tags.split(',').map((tag) => tag.trim()).filter(Boolean).map((tag) => [tag.toLocaleLowerCase(), tag])).values()],
  }
}

export function ResourceEditorPage({ mode }: { mode: LibraryMode }) {
  const { resourceId } = useParams()
  const [params] = useSearchParams()
  const navigate = useNavigate()
  const { accessToken } = useAuth()
  const initialKind = (params.get('kind') as LibraryResourceKind | null) ?? 'Book'
  const [detail, setDetail] = useState<LibraryResourceDetail | null>(null)
  const [form, setForm] = useState<FormState>(() => emptyForm(initialKind, mode))
  const [storage, setStorage] = useState<LibraryStorageUsage | null>(null)
  const [loading, setLoading] = useState(Boolean(resourceId))
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const base = mode === 'admin' ? '/admin/library' : '/library'

  const load = useCallback(async () => {
    if (!accessToken || !resourceId) return
    try {
      const result = await libraryApi.getManagedResource(mode, resourceId, accessToken)
      setDetail(result)
      setForm(fromDetail(result))
    } catch (reason) {
      setError(errorMessage(reason, 'Unable to load this resource for editing.'))
    } finally {
      setLoading(false)
    }
  }, [accessToken, mode, resourceId])

  useEffect(() => {
    if (!accessToken || !resourceId) return
    let active = true
    libraryApi.getManagedResource(mode, resourceId, accessToken)
      .then((result) => {
        if (!active) return
        setDetail(result)
        setForm(fromDetail(result))
      })
      .catch((reason: unknown) => active && setError(errorMessage(reason, 'Unable to load this resource for editing.')))
      .finally(() => active && setLoading(false))
    return () => { active = false }
  }, [accessToken, mode, resourceId])
  useEffect(() => {
    if (accessToken && mode === 'personal') libraryApi.getStorage(accessToken).then(setStorage).catch(() => undefined)
  }, [accessToken, mode, detail])

  function set<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm((current) => ({ ...current, [key]: value }))
  }

  async function save() {
    if (!accessToken) return
    setSaving(true)
    setError('')
    setMessage('')
    try {
      const input = toInput(form)
      if (detail) {
        const updated = await libraryApi.updateResource(mode, detail.resource.id, input, accessToken)
        setDetail(updated)
        setForm(fromDetail(updated))
        setMessage('Saved.')
      } else {
        const created = await libraryApi.createResource(mode, input, accessToken)
        navigate(`${base}/${mode === 'admin' ? '' : 'resources/'}${created.resource.id}/edit`, { replace: true })
        setMessage(form.kind === 'Book' || form.kind === 'File' ? 'Created. You can now upload files and add chapters.' : 'Created.')
      }
    } catch (reason) {
      setError(errorMessage(reason, 'Unable to save this resource.'))
    } finally {
      setSaving(false)
    }
  }

  async function action(run: () => Promise<unknown>, success: string) {
    setError('')
    setMessage('')
    try {
      await run()
      setMessage(success)
      await load()
    } catch (reason) {
      setError(errorMessage(reason, 'The action could not be completed.'))
    }
  }

  async function remove() {
    if (!accessToken || !detail || !window.confirm('Delete this resource, its files, chapters, and every learner bookmark, note and progress record for it?')) return
    try {
      await libraryApi.deleteResource(mode, detail.resource.id, accessToken)
      navigate(mode === 'admin' ? '/admin/library' : '/library')
    } catch (reason) {
      setError(errorMessage(reason, 'Unable to delete this resource.'))
    }
  }

  if (!accessToken) return <SignInRequired />
  if (loading) return <LoadingState label="Loading resource..." />

  const isNew = !detail
  const rightsOptions = mode === 'admin' ? sharedRights : (Object.keys(rightsLabels) as LibraryRightsBasis[])
  const needsNote = mode === 'admin' && notedRights.includes(form.rightsBasis)
  const resource = detail?.resource

  return (
    <>
      <Link to={mode === 'admin' ? '/admin/library' : resource ? `/library/resources/${resource.id}` : '/library'} className="mb-4 inline-flex items-center gap-1 text-sm text-slate-500 hover:text-slate-800 dark:hover:text-slate-200"><ArrowLeft className="h-4 w-4" />Back</Link>
      <PageHeader
        title={isNew ? `Add ${mode === 'admin' ? 'shared' : 'private'} resource` : `Edit ${resource?.title}`}
        description={mode === 'admin'
          ? 'Shared resources are visible to every signed-in learner once published. Only upload files you have the right to redistribute.'
          : 'Private resources are visible only to you. Personal copies of copyrighted books belong here, never in the shared library.'}
        action={resource && <div className="flex flex-wrap items-center gap-2"><VisibilityBadge resource={resource} />
          {mode === 'admin' && resource.status !== 'Published' && <Button size="sm" onClick={() => void action(() => libraryApi.setStatus(resource.id, 'publish', accessToken), 'Published to learners.')}><Send className="h-4 w-4" />Publish</Button>}
          {mode === 'admin' && resource.status !== 'Archived' && <Button size="sm" variant="secondary" onClick={() => void action(() => libraryApi.setStatus(resource.id, 'archive', accessToken), 'Archived and hidden from learners.')}><Archive className="h-4 w-4" />Archive</Button>}
          {mode === 'personal' && <Link to={`/library/resources/${resource.id}`}><Button size="sm" variant="secondary"><Eye className="h-4 w-4" />View</Button></Link>}
          {(mode === 'personal' || resource.status !== 'Published') && <Button size="sm" variant="ghost" onClick={() => void remove()}><Trash2 className="h-4 w-4" />Delete</Button>}
        </div>}
      />
      {error && <div className="mb-4"><ErrorState title="Not saved" description={error} /></div>}
      {message && <p className="mb-4 rounded-lg bg-emerald-50 px-4 py-3 text-sm text-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-300" role="status">{message}</p>}

      <div className="grid gap-5 xl:grid-cols-[minmax(0,1.4fr)_minmax(0,1fr)]">
        <Card className="p-5">
          <form className="space-y-4" onSubmit={(event) => { event.preventDefault(); void save() }}>
            <div className="grid gap-3 sm:grid-cols-2">
              <label className={labelClass}>Kind<select className={fieldClass} value={form.kind} disabled={!isNew} onChange={(event) => setForm(emptyForm(event.target.value as LibraryResourceKind, mode))}>{Object.entries(kindLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
              <label className={labelClass}>Category<select className={fieldClass} value={form.category} onChange={(event) => set('category', event.target.value as LibraryResourceCategory)}>{Object.entries(categoryLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
            </div>
            <label className={labelClass}>Title<input required maxLength={300} className={fieldClass} value={form.title} onChange={(event) => set('title', event.target.value)} /></label>
            <label className={labelClass}>{form.kind === 'Book' ? 'Authors' : 'Author(s) / creator'}<input maxLength={500} className={fieldClass} value={form.authors} onChange={(event) => set('authors', event.target.value)} /></label>
            <label className={labelClass}>Description<textarea maxLength={4000} rows={3} className="mt-1 w-full rounded-lg border border-slate-200 bg-white p-3 text-sm dark:border-slate-700 dark:bg-slate-950" value={form.description} onChange={(event) => set('description', event.target.value)} /></label>

            {form.kind === 'Book' && <fieldset className="grid gap-3 rounded-lg border border-slate-200 p-4 sm:grid-cols-3 dark:border-slate-700"><legend className="px-1 text-xs font-semibold">Book details</legend>
              <label className={`${labelClass} sm:col-span-2`}>Publisher<input maxLength={200} className={fieldClass} value={form.publisher} onChange={(event) => set('publisher', event.target.value)} /></label>
              <label className={labelClass}>Edition<input maxLength={80} className={fieldClass} value={form.edition} onChange={(event) => set('edition', event.target.value)} /></label>
              <label className={labelClass}>Publication year<input inputMode="numeric" className={fieldClass} value={form.publicationYear} onChange={(event) => set('publicationYear', event.target.value)} /></label>
              <label className={labelClass}>ISBN (optional)<input maxLength={20} className={fieldClass} value={form.isbn} onChange={(event) => set('isbn', event.target.value)} /></label>
              <label className={labelClass}>Pages<input inputMode="numeric" className={fieldClass} value={form.pageCount} onChange={(event) => set('pageCount', event.target.value)} /></label>
              <label className={labelClass}>Language<input maxLength={60} className={fieldClass} value={form.language} onChange={(event) => set('language', event.target.value)} /></label>
            </fieldset>}

            {form.kind === 'Note' && <label className={labelClass}>Note content<textarea required maxLength={100000} rows={12} className="mt-1 w-full rounded-lg border border-slate-200 bg-white p-3 text-sm leading-6 dark:border-slate-700 dark:bg-slate-950" value={form.noteContent} onChange={(event) => set('noteContent', event.target.value)} /></label>}

            <fieldset className="grid gap-3 rounded-lg border border-slate-200 p-4 sm:grid-cols-2 dark:border-slate-700"><legend className="px-1 text-xs font-semibold">Source</legend>
              <label className={labelClass}>Source / publisher name<input maxLength={200} className={fieldClass} value={form.sourceName} onChange={(event) => set('sourceName', event.target.value)} placeholder="e.g. Ministry of Finance" /></label>
              <label className={labelClass}>Source type<select className={fieldClass} value={form.sourceType} onChange={(event) => set('sourceType', event.target.value as LibrarySourceType)}>{Object.entries(sourceTypeLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
              <label className={labelClass}>Website<input maxLength={200} className={fieldClass} value={form.website} onChange={(event) => set('website', event.target.value)} placeholder="e.g. mof.gov.bd" /></label>
              <label className={labelClass}>Publication date<input type="date" className={fieldClass} value={form.publicationDate} onChange={(event) => set('publicationDate', event.target.value)} /></label>
              <label className={`${labelClass} sm:col-span-2`}>URL{form.kind === 'ExternalLink' ? ' (required)' : ''}<input type="url" required={form.kind === 'ExternalLink'} maxLength={1024} className={fieldClass} value={form.url} onChange={(event) => set('url', event.target.value)} placeholder="https://" /></label>
            </fieldset>

            <fieldset className="space-y-3 rounded-lg border border-slate-200 p-4 dark:border-slate-700"><legend className="px-1 text-xs font-semibold">Copyright and rights</legend>
              <label className={labelClass}>Rights basis for any uploaded file<select className={fieldClass} value={form.rightsBasis} onChange={(event) => set('rightsBasis', event.target.value as LibraryRightsBasis)}>{rightsOptions.map((value) => <option key={value} value={value}>{rightsLabels[value].label}</option>)}</select></label>
              <p className="text-xs text-slate-500">{rightsLabels[form.rightsBasis].help}{mode === 'admin' && ' Personal copies cannot be added to the shared library; link to the publisher instead.'}</p>
              {(needsNote || form.rightsNote) && <label className={labelClass}>Licence, permission or publication terms{needsNote ? ' (required)' : ''}<input maxLength={1000} className={fieldClass} value={form.rightsNote} onChange={(event) => set('rightsNote', event.target.value)} placeholder="e.g. CC BY 4.0, or 'free download at plancomm.gov.bd'" /></label>}
            </fieldset>

            <div><p className={labelClass}>Syllabus subjects and topics</p><div className="mt-1"><SyllabusMappingPicker subjects={form.subjects} topics={form.topics} onSubjectsChange={(value) => set('subjects', value)} onTopicsChange={(value) => set('topics', value)} /></div></div>
            <label className={labelClass}>Tags (comma separated, up to 15)<input className={fieldClass} value={form.tags} onChange={(event) => set('tags', event.target.value)} placeholder="constitution, 46th BCS, revision" /></label>
            <div className="flex justify-end"><Button type="submit" disabled={saving || !form.title.trim()}><Save className="h-4 w-4" />{saving ? 'Saving...' : isNew ? 'Create resource' : 'Save changes'}</Button></div>
          </form>
        </Card>

        <div className="space-y-5">
          {detail && (detail.resource.kind === 'Book' || detail.resource.kind === 'File') && <FilesCard detail={detail} mode={mode} token={accessToken} storage={storage} onChanged={load} onError={setError} />}
          {detail?.resource.kind === 'Book' && <ChaptersCard detail={detail} mode={mode} token={accessToken} onChanged={load} onError={setError} />}
          {isNew && <Card className="p-5 text-sm text-slate-500">Save the resource first; then you can upload its file{form.kind === 'Book' ? ', a cover image, and chapters' : ''}.</Card>}
        </div>
      </div>
    </>
  )
}

function FilesCard({ detail, mode, token, storage, onChanged, onError }: { detail: LibraryResourceDetail; mode: LibraryMode; token: string; storage: LibraryStorageUsage | null; onChanged: () => Promise<void>; onError: (value: string) => void }) {
  const [uploading, setUploading] = useState<LibraryFileRole | null>(null)
  const roles: LibraryFileRole[] = detail.resource.kind === 'Book' ? ['Primary', 'Cover'] : ['Primary']
  const accept = '.pdf,.png,.jpg,.jpeg,.webp,.gif,.docx,.xlsx,.pptx,.txt,.md,.csv'

  async function upload(role: LibraryFileRole, file: File | undefined) {
    if (!file) return
    if (storage && file.size > storage.maxFileSizeBytes) {
      onError(`Files cannot exceed ${formatBytes(storage.maxFileSizeBytes)}.`)
      return
    }
    setUploading(role)
    onError('')
    try {
      await libraryApi.uploadFile(mode, detail.resource.id, role, file, token)
      await onChanged()
    } catch (reason) {
      onError(errorMessage(reason, 'Upload failed.'))
    } finally {
      setUploading(null)
    }
  }

  return (
    <Card className="p-5">
      <CardHeader title="Files" subtitle={storage ? `Private storage: ${formatBytes(storage.usedBytes)} of ${formatBytes(storage.quotaBytes)} · max ${formatBytes(storage.maxFileSizeBytes)} per file` : 'Files are verified by content signature, hashed, and served only to authorised users.'} />
      <div className="mt-4 space-y-4">
        {roles.map((role) => {
          const existing = detail.files.find((file) => file.role === role)
          return <div key={role} className="rounded-lg border border-slate-200 p-3 dark:border-slate-700">
            <p className="text-sm font-medium">{role === 'Primary' ? (detail.resource.kind === 'Book' ? 'Digital copy' : 'File') : 'Cover image'}</p>
            {existing ? <div className="mt-1 flex items-center justify-between gap-2 text-xs text-slate-500"><span className="min-w-0 truncate">{existing.originalFileName} · {existing.format} · {formatBytes(existing.sizeBytes)}</span><button type="button" className="shrink-0 text-red-600 hover:underline" onClick={() => window.confirm('Remove this file?') && void libraryApi.deleteFile(mode, detail.resource.id, existing.id, token).then(onChanged).catch((reason: unknown) => onError(errorMessage(reason, 'Unable to remove the file.')))}>Remove</button></div> : <p className="mt-1 text-xs text-slate-500">No file uploaded.</p>}
            <label className="mt-2 inline-flex cursor-pointer items-center gap-2 rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-medium hover:bg-slate-50 dark:border-slate-700 dark:hover:bg-slate-800"><Upload className="h-3.5 w-3.5" />{uploading === role ? 'Uploading...' : existing ? 'Replace' : 'Upload'}<input type="file" className="sr-only" disabled={uploading !== null} accept={role === 'Cover' ? '.png,.jpg,.jpeg,.webp,.gif' : accept} onChange={(event) => { void upload(role, event.target.files?.[0]); event.target.value = '' }} /></label>
          </div>
        })}
        <p className="text-xs text-slate-500">Accepted: PDF, PNG, JPEG, WEBP, GIF, DOCX, XLSX, PPTX, TXT, MD, CSV. SVG, HTML, macro-enabled and legacy Office files are rejected. Rights basis: <strong>{rightsLabels[detail.rightsBasis].label}</strong>.</p>
      </div>
    </Card>
  )
}

interface ChapterForm { id: string | null; chapterNumber: string; title: string; summary: string; startPage: string; endPage: string; topics: PickedRef[]; linkedIds: string[] }

function ChaptersCard({ detail, mode, token, onChanged, onError }: { detail: LibraryResourceDetail; mode: LibraryMode; token: string; onChanged: () => Promise<void>; onError: (value: string) => void }) {
  const nextNumber = (detail.chapters.at(-1)?.chapterNumber ?? 0) + 1
  const blank = useMemo<ChapterForm>(() => ({ id: null, chapterNumber: String(nextNumber), title: '', summary: '', startPage: '', endPage: '', topics: [], linkedIds: [] }), [nextNumber])
  const [form, setForm] = useState<ChapterForm | null>(null)
  const [candidates, setCandidates] = useState<LibraryResourceSummary[]>([])

  useEffect(() => {
    if (!form) return
    let active = true
    const request = mode === 'admin'
      ? libraryApi.getAdminResources({ pageSize: 100 }, token)
      : libraryApi.getResources({ pageSize: 100, sortBy: 'title' }, token)
    request.then((result) => active && setCandidates(result.items.filter((item) => item.id !== detail.resource.id))).catch(() => undefined)
    return () => { active = false }
  }, [form, mode, token, detail.resource.id])

  function edit(chapter: LibraryChapter) {
    setForm({
      id: chapter.id,
      chapterNumber: String(chapter.chapterNumber),
      title: chapter.title,
      summary: chapter.summary ?? '',
      startPage: chapter.startPage?.toString() ?? '',
      endPage: chapter.endPage?.toString() ?? '',
      topics: chapter.topics.map((topic) => ({ id: topic.id, label: `${topic.title} (${topic.subjectName})` })),
      linkedIds: chapter.linkedResources.map((linked) => linked.id),
    })
  }

  async function save() {
    if (!form) return
    onError('')
    const input = {
      chapterNumber: Number.parseInt(form.chapterNumber, 10),
      title: form.title.trim(),
      summary: text(form.summary),
      startPage: integer(form.startPage),
      endPage: integer(form.endPage),
      syllabusItemIds: form.topics.map((topic) => topic.id),
      linkedResourceIds: form.linkedIds,
    }
    try {
      if (form.id) await libraryApi.updateChapter(mode, detail.resource.id, form.id, input, token)
      else await libraryApi.addChapter(mode, detail.resource.id, input, token)
      setForm(null)
      await onChanged()
    } catch (reason) {
      onError(errorMessage(reason, 'Unable to save the chapter.'))
    }
  }

  return (
    <Card className="p-5">
      <CardHeader title="Chapters" subtitle="Map chapters to syllabus topics and attach chapter-wise resources." action={!form && <Button size="sm" variant="secondary" onClick={() => setForm(blank)}><Plus className="h-4 w-4" />Add chapter</Button>} />
      <ul className="mt-3 divide-y divide-slate-100 dark:divide-slate-800">{detail.chapters.map((chapter) => <li key={chapter.id} className="flex items-start justify-between gap-2 py-2 text-sm"><button type="button" className="min-w-0 text-left hover:underline" onClick={() => edit(chapter)}><span className="text-xs text-slate-500">Ch. {chapter.chapterNumber}{chapter.startPage ? ` · p. ${chapter.startPage}${chapter.endPage ? `–${chapter.endPage}` : ''}` : ''}</span><span className="block">{chapter.title}</span><span className="block text-xs text-slate-500">{chapter.topics.length} topics · {chapter.linkedResources.length} resources</span></button><button type="button" aria-label={`Delete chapter ${chapter.chapterNumber}`} className="text-slate-400 hover:text-red-600" onClick={() => window.confirm('Delete this chapter?') && void libraryApi.deleteChapter(mode, detail.resource.id, chapter.id, token).then(onChanged)}><Trash2 className="h-4 w-4" /></button></li>)}{detail.chapters.length === 0 && <li className="py-2 text-xs text-slate-500">No chapters yet.</li>}</ul>
      {form && <div className="mt-4 space-y-3 rounded-lg border border-slate-200 p-4 dark:border-slate-700">
        <div className="grid grid-cols-3 gap-2">
          <label className={labelClass}>Number<input inputMode="numeric" className={fieldClass} value={form.chapterNumber} onChange={(event) => setForm({ ...form, chapterNumber: event.target.value })} /></label>
          <label className={labelClass}>Start page<input inputMode="numeric" className={fieldClass} value={form.startPage} onChange={(event) => setForm({ ...form, startPage: event.target.value })} /></label>
          <label className={labelClass}>End page<input inputMode="numeric" className={fieldClass} value={form.endPage} onChange={(event) => setForm({ ...form, endPage: event.target.value })} /></label>
        </div>
        <label className={labelClass}>Title<input maxLength={300} className={fieldClass} value={form.title} onChange={(event) => setForm({ ...form, title: event.target.value })} /></label>
        <label className={labelClass}>Summary<textarea maxLength={4000} rows={3} className="mt-1 w-full rounded-lg border border-slate-200 bg-white p-2 text-sm dark:border-slate-700 dark:bg-slate-950" value={form.summary} onChange={(event) => setForm({ ...form, summary: event.target.value })} /></label>
        <div><p className={labelClass}>Chapter topics</p><div className="mt-1"><SyllabusMappingPicker subjects={[]} topics={form.topics} showSubjects={false} onTopicsChange={(value) => setForm({ ...form, topics: value })} /></div></div>
        <label className={labelClass}>Chapter resources{mode === 'admin' ? ' (shared only)' : ''}<select multiple className="mt-1 h-32 w-full rounded-lg border border-slate-200 bg-white p-1 text-sm dark:border-slate-700 dark:bg-slate-950" value={form.linkedIds} onChange={(event) => setForm({ ...form, linkedIds: [...event.target.selectedOptions].map((option) => option.value) })}>{candidates.map((candidate) => <option key={candidate.id} value={candidate.id}>{kindLabels[candidate.kind]}: {candidate.title}</option>)}</select></label>
        <div className="flex justify-end gap-2"><Button size="sm" variant="ghost" onClick={() => setForm(null)}>Cancel</Button><Button size="sm" disabled={!form.title.trim() || !form.chapterNumber} onClick={() => void save()}><Save className="h-4 w-4" />Save chapter</Button></div>
      </div>}
    </Card>
  )
}
