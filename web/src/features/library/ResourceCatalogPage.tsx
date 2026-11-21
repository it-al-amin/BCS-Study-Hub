import { Bookmark, Plus, Search, X } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { Table } from '../../components/ui/Table'
import type { PagedResult } from '../../services/api'
import {
  libraryApi,
  type LibraryFileFormat,
  type LibraryResourceCategory,
  type LibraryResourceKind,
  type LibraryResourceQuery,
  type LibraryResourceSummary,
} from '../../services/library'
import { syllabusApi, type SyllabusSubject } from '../../services/syllabus'
import { SignInRequired } from './LibraryPage'
import { categoryLabels, errorMessage, kindLabels } from './libraryFormat'
import { KindIcon, ProtectedImage, VisibilityBadge } from './libraryUi'

const selectClass = 'h-10 rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-800'

export function ResourceCatalogPage({ fixedKind }: { fixedKind?: LibraryResourceKind }) {
  const { accessToken } = useAuth()
  const [params, setParams] = useSearchParams()
  const [result, setResult] = useState<PagedResult<LibraryResourceSummary> | null>(null)
  const [tags, setTags] = useState<string[]>([])
  const [subjects, setSubjects] = useState<SyllabusSubject[]>([])
  const [searchDraft, setSearchDraft] = useState(params.get('search') ?? '')
  const [error, setError] = useState('')
  const [loadedKey, setLoadedKey] = useState<string | null>(null)

  const query = useMemo<LibraryResourceQuery>(() => ({
    search: params.get('search') ?? undefined,
    kind: fixedKind ?? ((params.get('kind') as LibraryResourceKind | null) ?? ''),
    category: (params.get('category') as LibraryResourceCategory | null) ?? '',
    format: (params.get('format') as LibraryFileFormat | null) ?? '',
    subjectId: params.get('subjectId') ?? undefined,
    syllabusItemId: params.get('syllabusItemId') ?? undefined,
    tag: params.get('tag') ?? undefined,
    scope: (params.get('scope') as LibraryResourceQuery['scope']) ?? 'all',
    bookmarkedOnly: params.get('bookmarkedOnly') === 'true',
    sortBy: (params.get('sortBy') as LibraryResourceQuery['sortBy']) ?? 'recent',
    pageNumber: Number(params.get('page') ?? '1') || 1,
    pageSize: fixedKind === 'Book' ? 24 : 20,
  }), [params, fixedKind])

  useEffect(() => {
    if (!accessToken) return
    let active = true
    Promise.all([libraryApi.getTags(accessToken), syllabusApi.getVersions({ pageSize: 50 })])
      .then(async ([tagResult, versions]) => {
        if (!active) return
        setTags(tagResult)
        const subjectLists = await Promise.all(versions.items.map((version) => syllabusApi.getTree(version.id).then((tree) => tree.subjects).catch(() => [])))
        const unique = new Map<string, SyllabusSubject>()
        subjectLists.flat().forEach((subject) => unique.set(subject.subjectId, subject))
        if (active) setSubjects([...unique.values()].sort((a, b) => a.subjectName.localeCompare(b.subjectName)))
      })
      .catch(() => undefined)
    return () => { active = false }
  }, [accessToken])

  const queryKey = JSON.stringify(query)
  useEffect(() => {
    if (!accessToken) return
    let active = true
    libraryApi.getResources(query, accessToken)
      .then((value) => {
        if (!active) return
        setResult(value)
        setError('')
        setLoadedKey(queryKey)
      })
      .catch((reason: unknown) => {
        if (!active) return
        setError(errorMessage(reason, 'Unable to load resources.'))
        setLoadedKey(queryKey)
      })
    return () => { active = false }
  }, [accessToken, query, queryKey])
  const loading = loadedKey !== queryKey

  function update(key: string, value: string | null) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.delete('page')
    setParams(next)
  }

  const activeFilterCount = ['kind', 'category', 'format', 'subjectId', 'syllabusItemId', 'tag', 'search', 'bookmarkedOnly'].filter((key) => params.get(key)).length + (query.scope !== 'all' ? 1 : 0)
  const title = fixedKind === 'Book' ? 'Book Catalog' : 'Resources'
  if (!accessToken) return <><PageHeader title={title} /><SignInRequired /></>

  return (
    <>
      <PageHeader
        title={title}
        description={fixedKind === 'Book' ? 'Books with editions, chapters, reading progress and topic mappings.' : 'Search and filter every resource you can access: shared library items and your private uploads.'}
        action={<Link to={`/library/new${fixedKind ? `?kind=${fixedKind}` : ''}`}><Button><Plus className="h-4 w-4" />{fixedKind === 'Book' ? 'Add book' : 'Add resource'}</Button></Link>}
      />
      <Card className="mb-5 p-4">
        <form className="flex flex-col gap-2 sm:flex-row" onSubmit={(event) => { event.preventDefault(); update('search', searchDraft.trim() || null) }}>
          <div className="relative flex-1"><Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" /><input aria-label="Search resources" value={searchDraft} onChange={(event) => setSearchDraft(event.target.value)} placeholder="Title, author, source, ISBN or exact tag" className="h-10 w-full rounded-lg border border-slate-200 bg-white pl-9 pr-3 text-sm dark:border-slate-700 dark:bg-slate-800" /></div>
          <Button type="submit" variant="secondary" disabled={searchDraft.trim().length === 1}>Search</Button>
        </form>
        <div className="mt-3 grid gap-2 sm:grid-cols-2 lg:grid-cols-4">
          {!fixedKind && <select aria-label="Kind" className={selectClass} value={query.kind} onChange={(event) => update('kind', event.target.value)}><option value="">All kinds</option>{Object.entries(kindLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select>}
          <select aria-label="Category" className={selectClass} value={query.category} onChange={(event) => update('category', event.target.value)}><option value="">All categories</option>{Object.entries(categoryLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select>
          <select aria-label="File format" className={selectClass} value={query.format} onChange={(event) => update('format', event.target.value)}><option value="">Any file format</option><option value="Pdf">PDF</option><option value="Image">Image</option><option value="Document">Office document</option><option value="Text">Text</option></select>
          <select aria-label="Subject" className={selectClass} value={query.subjectId ?? ''} onChange={(event) => update('subjectId', event.target.value)}><option value="">All subjects</option>{subjects.map((subject) => <option key={subject.subjectId} value={subject.subjectId}>{subject.subjectName}</option>)}</select>
          <select aria-label="Tag" className={selectClass} value={query.tag ?? ''} onChange={(event) => update('tag', event.target.value)}><option value="">Any tag</option>{tags.map((tag) => <option key={tag} value={tag}>{tag}</option>)}</select>
          <select aria-label="Scope" className={selectClass} value={query.scope} onChange={(event) => update('scope', event.target.value === 'all' ? null : event.target.value)}><option value="all">Shared and private</option><option value="shared">Shared library only</option><option value="mine">My private resources</option></select>
          <select aria-label="Sort" className={selectClass} value={query.sortBy} onChange={(event) => update('sortBy', event.target.value === 'recent' ? null : event.target.value)}><option value="recent">Recently updated</option><option value="added">Recently added</option><option value="title">Title A–Z</option><option value="published">Publication date</option></select>
          <label className="flex h-10 items-center gap-2 rounded-lg border border-slate-200 px-3 text-sm dark:border-slate-700"><input type="checkbox" checked={query.bookmarkedOnly} onChange={(event) => update('bookmarkedOnly', event.target.checked ? 'true' : null)} /><Bookmark className="h-4 w-4" />Bookmarked only</label>
        </div>
        {activeFilterCount > 0 && <button type="button" className="mt-3 inline-flex items-center gap-1 text-xs text-slate-500 hover:text-slate-800 dark:hover:text-slate-200" onClick={() => { setSearchDraft(''); setParams(new URLSearchParams()) }}><X className="h-3 w-3" />Clear {activeFilterCount} filter{activeFilterCount === 1 ? '' : 's'}</button>}
        {params.get('syllabusItemId') && <p className="mt-2 text-xs text-slate-500">Filtered to resources and chapters mapped to a syllabus topic.</p>}
      </Card>
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {loading && !result ? <LoadingState label="Loading resources..." />
        : result && result.items.length === 0 ? <Card><EmptyState title="No matching resources" description="Adjust the filters, or add a resource to your private library." /></Card>
          : result && <Card className="overflow-hidden">
            {fixedKind === 'Book' ? <BookGrid items={result.items} token={accessToken} /> : <ResourceTable items={result.items} />}
            <Pagination page={result.pageNumber} totalPages={Math.max(result.totalPages, 1)} onPageChange={(page) => update('page', String(page))} />
          </Card>}
    </>
  )
}

function ResourceTable({ items }: { items: LibraryResourceSummary[] }) {
  return (
    <Table headers={['Resource', 'Category', 'Mapped to', 'Access', 'Progress']}>
      {items.map((resource) => <tr key={resource.id} className="hover:bg-slate-50 dark:hover:bg-slate-800/40">
        <td className="px-5 py-3"><Link to={`/library/resources/${resource.id}`} className="flex items-start gap-3"><span className="mt-0.5"><KindIcon resource={resource} /></span><span className="min-w-0"><span className="block font-medium hover:underline">{resource.title}</span><span className="block truncate text-xs text-slate-500">{[kindLabels[resource.kind], resource.authors, resource.sourceName].filter(Boolean).join(' · ')}</span>{resource.tags.length > 0 && <span className="mt-1 flex flex-wrap gap-1">{resource.tags.slice(0, 4).map((tag) => <span key={tag} className="rounded bg-slate-100 px-1.5 py-0.5 text-[11px] text-slate-600 dark:bg-slate-800 dark:text-slate-300">{tag}</span>)}</span>}</span></Link></td>
        <td className="px-5 py-3 text-slate-500">{categoryLabels[resource.category]}</td>
        <td className="px-5 py-3 text-xs text-slate-500">{[...resource.subjects.map((subject) => subject.name), ...resource.topics.map((topic) => topic.title)].slice(0, 3).join(', ') || '—'}</td>
        <td className="px-5 py-3"><VisibilityBadge resource={resource} />{resource.isBookmarked && <span className="ml-1"><Badge tone="info">Bookmarked</Badge></span>}</td>
        <td className="px-5 py-3 text-xs text-slate-500">{resource.progress ? `${resource.progress.percentComplete}%` : '—'}</td>
      </tr>)}
    </Table>
  )
}

function BookGrid({ items, token }: { items: LibraryResourceSummary[]; token: string }) {
  return (
    <div className="grid gap-4 p-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
      {items.map((book) => <Link key={book.id} to={`/library/books/${book.id}`} className="group overflow-hidden rounded-xl border border-slate-200 hover:border-emerald-600 dark:border-slate-700">
        <ProtectedImage resourceId={book.id} fileId={book.coverFileId} token={token} alt={`Cover of ${book.title}`} className="aspect-[3/4] w-full" />
        <div className="p-3">
          <p className="line-clamp-2 text-sm font-medium group-hover:underline">{book.title}</p>
          <p className="mt-1 truncate text-xs text-slate-500">{book.authors ?? 'Unknown author'}</p>
          <p className="mt-1 truncate text-xs text-slate-500">{[book.book?.publisher, book.book?.edition, book.book?.publicationYear].filter(Boolean).join(' · ')}</p>
          <div className="mt-2 flex items-center justify-between gap-2"><VisibilityBadge resource={book} /><span className="text-xs text-slate-500">{book.chapterCount} ch.</span></div>
          {book.progress && <div className="mt-2 h-1.5 rounded-full bg-slate-100 dark:bg-slate-800"><div className="h-1.5 rounded-full bg-emerald-600" style={{ width: `${book.progress.percentComplete}%` }} /></div>}
        </div>
      </Link>)}
    </div>
  )
}
