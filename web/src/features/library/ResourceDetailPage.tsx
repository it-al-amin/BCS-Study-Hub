import { ArrowLeft, Bookmark, BookmarkPlus, Download, ExternalLink, FolderPlus, Pencil, Save, Settings, Trash2 } from 'lucide-react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { ApiError } from '../../services/api'
import {
  libraryApi,
  type LibraryChapter,
  type LibraryCollection,
  type LibraryNote,
  type LibraryReadingStatus,
  type LibraryResourceDetail,
} from '../../services/library'
import { SignInRequired } from './LibraryPage'
import {
  categoryLabels,
  downloadLibraryFile,
  errorMessage,
  formatBytes,
  kindLabels,
  readingStatusLabels,
  rightsLabels,
  sourceTypeLabels,
} from './libraryFormat'
import { FileViewer, KindIcon, ProtectedImage, VisibilityBadge } from './libraryUi'
import { Markdown } from '../../components/content/Markdown'

const inputClass = 'mt-1 h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950'

function toNumber(value: string) {
  const parsed = Number.parseInt(value, 10)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null
}

export function ResourceDetailPage() {
  const { resourceId = '' } = useParams()
  // Keyed so navigating between resources remounts with fresh state instead of resetting it in an effect.
  return <ResourceDetail key={resourceId} resourceId={resourceId} />
}

function ResourceDetail({ resourceId }: { resourceId: string }) {
  const [params] = useSearchParams()
  const { accessToken, user } = useAuth()
  const [detail, setDetail] = useState<LibraryResourceDetail | null>(null)
  const [collections, setCollections] = useState<LibraryCollection[]>([])
  const [selectedChapterId, setSelectedChapterId] = useState<string | null>(null)
  const [viewerPage, setViewerPage] = useState<number | null>(toNumber(params.get('page') ?? ''))
  const [pageDraft, setPageDraft] = useState(params.get('page') ?? '')
  const [error, setError] = useState('')
  const [notFound, setNotFound] = useState(false)
  const [message, setMessage] = useState('')

  const reload = useCallback(async () => {
    if (!accessToken) return
    try {
      setDetail(await libraryApi.getResource(resourceId, accessToken))
    } catch (reason) {
      if (reason instanceof ApiError && reason.status === 404) setNotFound(true)
      else setError(errorMessage(reason, 'Unable to load this resource.'))
    }
  }, [accessToken, resourceId])

  useEffect(() => {
    if (!accessToken) return
    let active = true
    libraryApi.getResource(resourceId, accessToken)
      .then((result) => active && setDetail(result))
      .catch((reason: unknown) => {
        if (!active) return
        if (reason instanceof ApiError && reason.status === 404) setNotFound(true)
        else setError(errorMessage(reason, 'Unable to load this resource.'))
      })
    libraryApi.getCollections(accessToken)
      .then((result) => active && setCollections(result))
      .catch(() => undefined)
    return () => { active = false }
  }, [accessToken, resourceId])

  const primaryFile = detail?.files.find((file) => file.role === 'Primary') ?? null
  const selectedChapter = detail?.chapters.find((chapter) => chapter.id === selectedChapterId) ?? null

  function goToPage(page: number | null) {
    setViewerPage(page)
    setPageDraft(page ? String(page) : '')
  }

  function selectChapter(chapter: LibraryChapter) {
    setSelectedChapterId(chapter.id)
    if (chapter.startPage) goToPage(chapter.startPage)
  }

  async function run(action: () => Promise<unknown>, success: string) {
    setError('')
    setMessage('')
    try {
      await action()
      setMessage(success)
      await reload()
    } catch (reason) {
      setError(errorMessage(reason, 'The action could not be completed.'))
    }
  }

  if (!accessToken) return <SignInRequired />
  if (notFound) return <Card><EmptyState title="Resource not found" description="It may be private to another user, unpublished, or deleted." /></Card>
  if (!detail) return error ? <ErrorState description={error} /> : <LoadingState label="Loading resource..." />

  const { resource } = detail
  const canManageShared = resource.visibility === 'Shared' && (user?.permissions.includes('library.manage') ?? false)
  const backHref = resource.kind === 'Book' ? '/books' : '/library/resources'

  return (
    <>
      <Link to={backHref} className="mb-4 inline-flex items-center gap-1 text-sm text-slate-500 hover:text-slate-800 dark:hover:text-slate-200"><ArrowLeft className="h-4 w-4" />Back to {resource.kind === 'Book' ? 'book catalog' : 'resources'}</Link>
      <div className="mb-6 flex flex-col gap-4 lg:flex-row lg:items-start">
        {resource.kind === 'Book' && <ProtectedImage resourceId={resource.id} fileId={resource.coverFileId} token={accessToken} alt={`Cover of ${resource.title}`} className="h-48 w-36 shrink-0 rounded-lg border border-slate-200 dark:border-slate-700" />}
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2"><KindIcon resource={resource} /><span className="text-xs uppercase tracking-wide text-slate-500">{kindLabels[resource.kind]} · {categoryLabels[resource.category]}</span><VisibilityBadge resource={resource} />{resource.isBookmarked && <Badge tone="info">Bookmarked</Badge>}</div>
          <h1 className="mt-2 text-2xl font-semibold tracking-tight">{resource.title}</h1>
          {resource.authors && <p className="mt-1 text-sm text-slate-600 dark:text-slate-300">{resource.authors}</p>}
          {resource.book && <p className="mt-1 text-sm text-slate-500">{[resource.book.publisher, resource.book.edition && `${resource.book.edition} edition`, resource.book.publicationYear, resource.book.isbn && `ISBN ${resource.book.isbn}`, resource.book.pageCount && `${resource.book.pageCount} pages`, resource.book.language].filter(Boolean).join(' · ')}</p>}
          {resource.description && <p className="mt-3 max-w-3xl whitespace-pre-line text-sm text-slate-600 dark:text-slate-300">{resource.description}</p>}
          <div className="mt-4 flex flex-wrap gap-2">
            <Button variant="secondary" size="sm" onClick={() => void run(() => libraryApi.addBookmark({ resourceId: resource.id, chapterId: null, page: null, label: null }, accessToken), 'Bookmarked.')}><BookmarkPlus className="h-4 w-4" />Bookmark</Button>
            <AddToCollection collections={collections} onAdd={(collectionId) => run(() => libraryApi.addCollectionItem(collectionId, resource.id, accessToken), 'Added to collection.')} />
            {resource.url && <a href={resource.url} target="_blank" rel="noopener noreferrer nofollow"><Button variant="secondary" size="sm"><ExternalLink className="h-4 w-4" />Open website</Button></a>}
            {primaryFile && <Button variant="secondary" size="sm" onClick={() => void downloadLibraryFile(resource.id, primaryFile, accessToken).catch((reason: unknown) => setError(errorMessage(reason, 'Download failed.')))}><Download className="h-4 w-4" />Download</Button>}
            {detail.canEdit && <Link to={`/library/resources/${resource.id}/edit`}><Button variant="secondary" size="sm"><Pencil className="h-4 w-4" />Edit</Button></Link>}
            {canManageShared && <Link to={`/admin/library/${resource.id}/edit`}><Button variant="secondary" size="sm"><Settings className="h-4 w-4" />Manage shared resource</Button></Link>}
          </div>
        </div>
      </div>
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {message && <p className="mb-4 rounded-lg bg-emerald-50 px-4 py-3 text-sm text-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-300" role="status">{message}</p>}

      <div className={`grid gap-5 ${detail.chapters.length > 0 ? 'xl:grid-cols-[16rem_minmax(0,1fr)_20rem]' : 'xl:grid-cols-[minmax(0,1fr)_20rem]'}`}>
        {detail.chapters.length > 0 && <Card className="h-fit overflow-hidden xl:sticky xl:top-20">
          <CardHeader title="Chapters" subtitle={`${detail.chapters.length} chapters`} />
          <nav aria-label="Chapter navigation" className="max-h-[70vh] overflow-auto">
            {detail.chapters.map((chapter) => <button key={chapter.id} type="button" onClick={() => selectChapter(chapter)} className={`block w-full border-b border-slate-100 px-4 py-2.5 text-left text-sm last:border-0 dark:border-slate-800 ${selectedChapterId === chapter.id ? 'bg-emerald-50 dark:bg-emerald-500/10' : 'hover:bg-slate-50 dark:hover:bg-slate-800/60'} ${resource.progress?.currentChapterId === chapter.id ? 'font-semibold' : ''}`}><span className="text-xs text-slate-500">Chapter {chapter.chapterNumber}{chapter.startPage ? ` · p. ${chapter.startPage}${chapter.endPage ? `–${chapter.endPage}` : ''}` : ''}</span><span className="block">{chapter.title}</span></button>)}
          </nav>
        </Card>}

        <div className="min-w-0 space-y-5">
          {selectedChapter && <Card className="p-5">
            <p className="text-xs uppercase tracking-wide text-slate-500">Chapter {selectedChapter.chapterNumber}</p>
            <h2 className="mt-1 text-lg font-semibold">{selectedChapter.title}</h2>
            {selectedChapter.summary && <p className="mt-2 whitespace-pre-line text-sm text-slate-600 dark:text-slate-300">{selectedChapter.summary}</p>}
            {selectedChapter.topics.length > 0 && <div className="mt-3 flex flex-wrap gap-1.5">{selectedChapter.topics.map((topic) => <Link key={topic.id} to={`/library/resources?syllabusItemId=${topic.id}`} className="rounded-full bg-sky-50 px-2.5 py-1 text-xs text-sky-800 hover:underline dark:bg-sky-500/10 dark:text-sky-300">{topic.title} · {topic.stageCode}</Link>)}</div>}
            {selectedChapter.linkedResources.length > 0 && <div className="mt-3"><p className="text-xs font-medium text-slate-500">Chapter resources</p><ul className="mt-1 space-y-1">{selectedChapter.linkedResources.map((linked) => <li key={linked.id}><Link to={`/library/resources/${linked.id}`} className="text-sm text-emerald-700 hover:underline dark:text-emerald-400">{linked.title}</Link> <span className="text-xs text-slate-500">{kindLabels[linked.kind]}</span></li>)}</ul></div>}
          </Card>}

          {primaryFile ? <Card className="p-4">
            <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
              <p className="text-sm font-medium">{primaryFile.originalFileName} <span className="text-xs font-normal text-slate-500">· {formatBytes(primaryFile.sizeBytes)}</span></p>
              {primaryFile.format === 'Pdf' && <form className="flex items-center gap-2" onSubmit={(event) => { event.preventDefault(); goToPage(toNumber(pageDraft)) }}><label className="text-xs text-slate-500" htmlFor="viewer-page">Go to page</label><input id="viewer-page" value={pageDraft} onChange={(event) => setPageDraft(event.target.value)} inputMode="numeric" className="h-8 w-20 rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950" /><Button size="sm" variant="secondary" type="submit">Go</Button></form>}
            </div>
            <FileViewer resourceId={resource.id} file={primaryFile} token={accessToken} page={viewerPage} />
          </Card>
            : resource.kind === 'Note' ? <Card className="p-5"><CardHeader title="Study note" /><Markdown source={detail.noteContent} className="mt-3" /></Card>
              : resource.kind === 'ExternalLink' ? <Card className="p-5"><p className="text-sm text-slate-600 dark:text-slate-300">This resource lives on an external website. Links open in a new tab; verify the publisher before relying on the content.</p>{resource.url && <a className="mt-3 inline-flex items-center gap-1 break-all text-sm text-emerald-700 hover:underline dark:text-emerald-400" href={resource.url} target="_blank" rel="noopener noreferrer nofollow"><ExternalLink className="h-4 w-4 shrink-0" />{resource.url}</a>}</Card>
                : <Card><EmptyState title="No file attached" description={resource.kind === 'Book' ? 'This book is catalogued without a digital copy. Track your reading of the physical book below.' : 'No file has been uploaded yet.'} /></Card>}

          {resource.kind !== 'Note' && detail.noteContent && <Card className="p-5"><CardHeader title="Notes" /><p className="mt-3 whitespace-pre-wrap text-sm">{detail.noteContent}</p></Card>}
          <NotesPanel detail={detail} token={accessToken} chapterId={selectedChapterId} page={viewerPage} onChanged={reload} onError={setError} />
        </div>

        <div className="space-y-5">
          <ProgressPanel detail={detail} token={accessToken} onSaved={reload} onError={setError} />
          <BookmarksPanel detail={detail} token={accessToken} chapterId={selectedChapterId} page={viewerPage} onJump={goToPage} onChanged={reload} onError={setError} />
          <Card className="p-5">
            <h2 className="font-semibold">Details</h2>
            <dl className="mt-3 space-y-2 text-sm">
              {resource.sourceName && <Detail label="Source">{resource.sourceName}</Detail>}
              <Detail label="Source type">{sourceTypeLabels[resource.sourceType]}</Detail>
              {detail.website && <Detail label="Website">{detail.website}</Detail>}
              {resource.publicationDate && <Detail label="Published">{new Date(resource.publicationDate).toLocaleDateString()}</Detail>}
              <Detail label="Rights">{rightsLabels[detail.rightsBasis].label}{detail.rightsNote ? ` — ${detail.rightsNote}` : ''}</Detail>
              <Detail label="Updated">{new Date(resource.updatedAt).toLocaleString()}</Detail>
            </dl>
            {(resource.subjects.length > 0 || resource.topics.length > 0) && <div className="mt-4"><p className="text-xs font-medium text-slate-500">Syllabus mapping</p><div className="mt-1.5 flex flex-wrap gap-1.5">{resource.subjects.map((subject) => <Link key={subject.id} to={`/library/resources?subjectId=${subject.id}`} className="rounded-full bg-emerald-50 px-2.5 py-1 text-xs text-emerald-800 hover:underline dark:bg-emerald-500/10 dark:text-emerald-300">{subject.name}</Link>)}{resource.topics.map((topic) => <Link key={topic.id} to={`/library/resources?syllabusItemId=${topic.id}`} title={`${topic.subjectName} · ${topic.stageCode} ${topic.versionLabel}`} className="rounded-full bg-sky-50 px-2.5 py-1 text-xs text-sky-800 hover:underline dark:bg-sky-500/10 dark:text-sky-300">{topic.title}</Link>)}</div></div>}
            {resource.tags.length > 0 && <div className="mt-4"><p className="text-xs font-medium text-slate-500">Tags</p><div className="mt-1.5 flex flex-wrap gap-1.5">{resource.tags.map((tag) => <Link key={tag} to={`/library/resources?tag=${encodeURIComponent(tag)}`} className="rounded bg-slate-100 px-2 py-0.5 text-xs hover:underline dark:bg-slate-800">{tag}</Link>)}</div></div>}
            {detail.files.length > 0 && <div className="mt-4"><p className="text-xs font-medium text-slate-500">Files</p><ul className="mt-1.5 space-y-2">{detail.files.map((file) => <li key={file.id} className="text-xs"><button type="button" className="font-medium text-emerald-700 hover:underline dark:text-emerald-400" onClick={() => void downloadLibraryFile(resource.id, file, accessToken)}>{file.originalFileName}</button><span className="block text-slate-500">{file.role} · {file.format} · {formatBytes(file.sizeBytes)}</span><span className="block truncate font-mono text-[10px] text-slate-400" title={file.sha256}>SHA-256 {file.sha256}</span></li>)}</ul></div>}
            {detail.usedInChapters.length > 0 && <div className="mt-4"><p className="text-xs font-medium text-slate-500">Used in chapters</p><ul className="mt-1.5 space-y-1">{detail.usedInChapters.map((ref) => <li key={ref.title}><Link to={`/library/books/${ref.id}`} className="text-xs text-emerald-700 hover:underline dark:text-emerald-400">{ref.title}</Link></li>)}</ul></div>}
          </Card>
        </div>
      </div>
    </>
  )
}

function Detail({ label, children }: { label: string; children: React.ReactNode }) {
  return <div className="flex justify-between gap-3"><dt className="text-slate-500">{label}</dt><dd className="text-right">{children}</dd></div>
}

function AddToCollection({ collections, onAdd }: { collections: LibraryCollection[]; onAdd: (collectionId: string) => Promise<void> }) {
  const [value, setValue] = useState('')
  if (collections.length === 0) return <Link to="/library/collections"><Button variant="secondary" size="sm"><FolderPlus className="h-4 w-4" />Create a collection</Button></Link>
  return <div className="flex items-center gap-1"><select aria-label="Collection" value={value} onChange={(event) => setValue(event.target.value)} className="h-8 rounded-lg border border-slate-200 bg-white px-2 text-xs dark:border-slate-700 dark:bg-slate-900"><option value="">Add to collection…</option>{collections.map((collection) => <option key={collection.id} value={collection.id}>{collection.name}</option>)}</select><Button size="sm" variant="secondary" disabled={!value} onClick={() => void onAdd(value).then(() => setValue(''))}><FolderPlus className="h-4 w-4" />Add</Button></div>
}

function ProgressPanel({ detail, token, onSaved, onError }: { detail: LibraryResourceDetail; token: string; onSaved: () => Promise<void>; onError: (value: string) => void }) {
  const progress = detail.resource.progress
  const [status, setStatus] = useState<LibraryReadingStatus>(progress?.status ?? 'Reading')
  const [chapterId, setChapterId] = useState(progress?.currentChapterId ?? '')
  const [currentPage, setCurrentPage] = useState(progress?.currentPage?.toString() ?? '')
  const [totalPages, setTotalPages] = useState((progress?.totalPages ?? detail.resource.book?.pageCount)?.toString() ?? '')
  const [percent, setPercent] = useState(progress?.percentComplete?.toString() ?? '')
  const [saving, setSaving] = useState(false)
  const pageBased = Boolean(totalPages)

  async function save() {
    setSaving(true)
    onError('')
    try {
      await libraryApi.updateProgress(detail.resource.id, {
        status,
        currentChapterId: chapterId || null,
        currentPage: toNumber(currentPage),
        totalPages: toNumber(totalPages),
        percentComplete: pageBased || percent === '' ? null : Math.min(100, Math.max(0, Number(percent))),
      }, token)
      await onSaved()
    } catch (reason) {
      onError(errorMessage(reason, 'Unable to save reading progress.'))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Card className="p-5">
      <h2 className="font-semibold">Reading progress</h2>
      {progress && <><div className="mt-3 h-2 rounded-full bg-slate-100 dark:bg-slate-800"><div className="h-2 rounded-full bg-emerald-600" style={{ width: `${progress.percentComplete}%` }} /></div><p className="mt-1 text-xs text-slate-500">{progress.percentComplete}% · {readingStatusLabels[progress.status]} · updated {new Date(progress.updatedAt).toLocaleDateString()}</p></>}
      <div className="mt-3 grid grid-cols-2 gap-2">
        <label className="col-span-2 text-xs text-slate-600 dark:text-slate-300">Status<select className={inputClass} value={status} onChange={(event) => setStatus(event.target.value as LibraryReadingStatus)}>{Object.entries(readingStatusLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
        {detail.chapters.length > 0 && <label className="col-span-2 text-xs text-slate-600 dark:text-slate-300">Current chapter<select className={inputClass} value={chapterId} onChange={(event) => setChapterId(event.target.value)}><option value="">—</option>{detail.chapters.map((chapter) => <option key={chapter.id} value={chapter.id}>{chapter.chapterNumber}. {chapter.title}</option>)}</select></label>}
        <label className="text-xs text-slate-600 dark:text-slate-300">Current page<input className={inputClass} inputMode="numeric" value={currentPage} onChange={(event) => setCurrentPage(event.target.value)} /></label>
        <label className="text-xs text-slate-600 dark:text-slate-300">Total pages<input className={inputClass} inputMode="numeric" value={totalPages} onChange={(event) => setTotalPages(event.target.value)} /></label>
        {!pageBased && <label className="col-span-2 text-xs text-slate-600 dark:text-slate-300">Percent complete<input className={inputClass} type="number" min={0} max={100} value={percent} onChange={(event) => setPercent(event.target.value)} /></label>}
      </div>
      <div className="mt-3 flex gap-2"><Button size="sm" disabled={saving} onClick={() => void save()}><Save className="h-4 w-4" />{saving ? 'Saving...' : 'Save progress'}</Button>{progress && <Button size="sm" variant="ghost" onClick={() => void libraryApi.clearProgress(detail.resource.id, token).then(onSaved).catch((reason: unknown) => onError(errorMessage(reason, 'Unable to clear progress.')))}>Clear</Button>}</div>
    </Card>
  )
}

function BookmarksPanel({ detail, token, chapterId, page, onJump, onChanged, onError }: { detail: LibraryResourceDetail; token: string; chapterId: string | null; page: number | null; onJump: (page: number | null) => void; onChanged: () => Promise<void>; onError: (value: string) => void }) {
  const [label, setLabel] = useState('')
  // null means "follow the page currently open in the viewer" until the user types their own page.
  const [pageDraft, setPageDraft] = useState<string | null>(null)
  const bookmarkPage = pageDraft ?? (page ? String(page) : '')
  const setBookmarkPage = setPageDraft

  async function add() {
    onError('')
    try {
      await libraryApi.addBookmark({ resourceId: detail.resource.id, chapterId, page: toNumber(bookmarkPage), label: label.trim() || null }, token)
      setLabel('')
      setPageDraft(null)
      await onChanged()
    } catch (reason) {
      onError(errorMessage(reason, 'Unable to add the bookmark.'))
    }
  }

  return (
    <Card className="p-5">
      <h2 className="font-semibold">Bookmarks</h2>
      <ul className="mt-3 space-y-1.5">{detail.bookmarks.map((bookmark) => <li key={bookmark.id} className="flex items-start justify-between gap-2 text-sm"><button type="button" className="min-w-0 text-left hover:underline" onClick={() => onJump(bookmark.page)}><Bookmark className="mr-1 inline h-3.5 w-3.5 text-emerald-600" />{bookmark.label ?? (bookmark.page ? `Page ${bookmark.page}` : 'Whole resource')}<span className="block text-xs text-slate-500">{[bookmark.chapterTitle, bookmark.page && `p. ${bookmark.page}`].filter(Boolean).join(' · ')}</span></button><button type="button" aria-label="Delete bookmark" className="text-slate-400 hover:text-red-600" onClick={() => void libraryApi.deleteBookmark(bookmark.id, token).then(onChanged)}><Trash2 className="h-4 w-4" /></button></li>)}{detail.bookmarks.length === 0 && <li className="text-xs text-slate-500">No bookmarks for this resource.</li>}</ul>
      <div className="mt-3 grid grid-cols-[5rem_1fr] gap-2"><input aria-label="Bookmark page" placeholder="Page" inputMode="numeric" className={inputClass} value={bookmarkPage} onChange={(event) => setBookmarkPage(event.target.value)} /><input aria-label="Bookmark label" placeholder="Label (optional)" maxLength={200} className={inputClass} value={label} onChange={(event) => setLabel(event.target.value)} /></div>
      <Button className="mt-2" size="sm" variant="secondary" onClick={() => void add()}><BookmarkPlus className="h-4 w-4" />Add bookmark{chapterId ? ' in chapter' : ''}</Button>
    </Card>
  )
}

function NotesPanel({ detail, token, chapterId, page, onChanged, onError }: { detail: LibraryResourceDetail; token: string; chapterId: string | null; page: number | null; onChanged: () => Promise<void>; onError: (value: string) => void }) {
  const [editing, setEditing] = useState<LibraryNote | null>(null)
  const [content, setContent] = useState('')
  const [attachLocation, setAttachLocation] = useState(true)
  const chapterTitle = useMemo(() => detail.chapters.find((chapter) => chapter.id === chapterId)?.title, [detail.chapters, chapterId])

  async function save() {
    onError('')
    try {
      const input = editing
        ? { chapterId: editing.chapterId, page: editing.page, content }
        : { chapterId: attachLocation ? chapterId : null, page: attachLocation ? page : null, content }
      if (editing) await libraryApi.updateNote(editing.id, input, token)
      else await libraryApi.createNote(detail.resource.id, input, token)
      setEditing(null)
      setContent('')
      await onChanged()
    } catch (reason) {
      onError(errorMessage(reason, 'Unable to save the note.'))
    }
  }

  return (
    <Card className="p-5">
      <CardHeader title="Personal notes" subtitle="Private to you. Never shared, even on shared resources." />
      <div className="mt-4 space-y-3">
        {detail.notes.map((note) => <article key={note.id} className="rounded-lg border border-slate-200 p-3 dark:border-slate-700"><div className="flex items-center justify-between gap-2 text-xs text-slate-500"><span>{[note.chapterTitle, note.page && `p. ${note.page}`, new Date(note.updatedAt).toLocaleString()].filter(Boolean).join(' · ')}</span><span className="flex gap-2"><button type="button" className="hover:text-slate-800 dark:hover:text-slate-200" onClick={() => { setEditing(note); setContent(note.content) }}>Edit</button><button type="button" className="hover:text-red-600" onClick={() => window.confirm('Delete this note?') && void libraryApi.deleteNote(note.id, token).then(onChanged)}>Delete</button></span></div><p className="mt-1.5 whitespace-pre-wrap text-sm">{note.content}</p></article>)}
        <textarea aria-label="Note" value={content} onChange={(event) => setContent(event.target.value)} maxLength={20000} rows={4} placeholder="Write a note about this resource…" className="w-full rounded-lg border border-slate-200 bg-white p-3 text-sm dark:border-slate-700 dark:bg-slate-950" />
        {!editing && (chapterId || page) && <label className="flex items-center gap-2 text-xs text-slate-500"><input type="checkbox" checked={attachLocation} onChange={(event) => setAttachLocation(event.target.checked)} />Attach to {[chapterTitle, page && `page ${page}`].filter(Boolean).join(', ')}</label>}
        <div className="flex gap-2"><Button size="sm" disabled={!content.trim()} onClick={() => void save()}><Save className="h-4 w-4" />{editing ? 'Update note' : 'Add note'}</Button>{editing && <Button size="sm" variant="ghost" onClick={() => { setEditing(null); setContent('') }}>Cancel</Button>}</div>
      </div>
    </Card>
  )
}
