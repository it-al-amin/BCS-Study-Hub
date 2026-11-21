import { BookMarked, BookOpen, FolderOpen, Plus, Search } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { libraryApi, type LibraryDashboard } from '../../services/library'
import { errorMessage, formatBytes, kindLabels, readingStatusLabels } from './libraryFormat'
import { KindIcon, VisibilityBadge } from './libraryUi'

export function SignInRequired() {
  return <Card><EmptyState title="Sign in to open your library" description="Library files are access-controlled. Private resources are visible only to you." /></Card>
}

export function LibraryPage() {
  const { accessToken } = useAuth()
  const navigate = useNavigate()
  const [dashboard, setDashboard] = useState<LibraryDashboard | null>(null)
  const [search, setSearch] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    if (!accessToken) return
    let active = true
    libraryApi.getDashboard(accessToken)
      .then((result) => active && setDashboard(result))
      .catch((reason: unknown) => active && setError(errorMessage(reason, 'Unable to load the library.')))
    return () => { active = false }
  }, [accessToken])

  if (!accessToken) return <><PageHeader title="Digital Library" /><SignInRequired /></>

  const usage = dashboard ? Math.min(100, Math.round((dashboard.storage.usedBytes / dashboard.storage.quotaBytes) * 100)) : 0
  return (
    <>
      <PageHeader
        title="Digital Library"
        description="Books, files, external references and study notes for long-term BCS preparation, mapped to syllabus topics."
        action={<Link to="/library/new"><Button><Plus className="h-4 w-4" />Add resource</Button></Link>}
      />
      <form className="mb-5 flex gap-2" onSubmit={(event) => { event.preventDefault(); navigate(`/library/resources?search=${encodeURIComponent(search.trim())}`) }}>
        <div className="relative flex-1"><Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" /><input aria-label="Search the library" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search titles, authors, ISBN, sources or tags" className="h-10 w-full rounded-lg border border-slate-200 bg-white pl-9 pr-3 text-sm dark:border-slate-700 dark:bg-slate-900" /></div>
        <Button type="submit" variant="secondary" disabled={search.trim().length < 2}>Search</Button>
      </form>
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {!dashboard ? (!error && <LoadingState label="Loading library..." />) : <>
        <div className="mb-5 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <Stat label="Resources available" value={dashboard.visibleResources} detail={`${dashboard.sharedResources} shared · ${dashboard.privateResources} private`} />
          <Stat label="Currently reading" value={dashboard.readingCount} detail={`${dashboard.completedCount} completed`} />
          <Stat label="Bookmarks" value={dashboard.bookmarkCount} detail={`${dashboard.collectionCount} collections`} />
          <Card className="p-5"><p className="text-xs font-medium uppercase tracking-wide text-slate-500">Private storage</p><p className="mt-2 text-2xl font-semibold">{formatBytes(dashboard.storage.usedBytes)}</p><div className="mt-2 h-1.5 rounded-full bg-slate-100 dark:bg-slate-800"><div className="h-1.5 rounded-full bg-emerald-600" style={{ width: `${usage}%` }} /></div><p className="mt-1 text-xs text-slate-500">of {formatBytes(dashboard.storage.quotaBytes)} · max {formatBytes(dashboard.storage.maxFileSizeBytes)} per file</p></Card>
        </div>
        <div className="mb-5 flex flex-wrap gap-2">
          {dashboard.byKind.map((entry) => <Link key={entry.kind} to={entry.kind === 'Book' ? '/books' : `/library/resources?kind=${entry.kind}`} className="rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm hover:border-emerald-600 dark:border-slate-700 dark:bg-slate-900">{kindLabels[entry.kind]} <span className="ml-1 text-slate-500">{entry.count}</span></Link>)}
          <Link to="/library/collections" className="rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm hover:border-emerald-600 dark:border-slate-700 dark:bg-slate-900"><FolderOpen className="mr-1 inline h-4 w-4" />Collections</Link>
          <Link to="/library/reading" className="rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm hover:border-emerald-600 dark:border-slate-700 dark:bg-slate-900"><BookMarked className="mr-1 inline h-4 w-4" />Reading & bookmarks</Link>
        </div>
        <div className="grid gap-5 xl:grid-cols-2">
          <Card><CardHeader title="Continue reading" action={<Link className="text-sm text-emerald-700 hover:underline dark:text-emerald-400" to="/library/reading">All progress</Link>} />
            {dashboard.continueReading.length === 0 ? <EmptyState title="Nothing in progress" description="Open a resource and record your reading progress to see it here." />
              : <ul className="divide-y divide-slate-100 dark:divide-slate-800">{dashboard.continueReading.map((item) => <li key={item.resourceId}><Link to={`/library/resources/${item.resourceId}`} className="block px-5 py-3 hover:bg-slate-50 dark:hover:bg-slate-800/60"><div className="flex items-center justify-between gap-3"><span className="truncate text-sm font-medium">{item.resourceTitle}</span><span className="text-xs text-slate-500">{readingStatusLabels[item.status]} · {item.percentComplete}%</span></div><div className="mt-2 h-1.5 rounded-full bg-slate-100 dark:bg-slate-800"><div className="h-1.5 rounded-full bg-emerald-600" style={{ width: `${item.percentComplete}%` }} /></div><p className="mt-1 text-xs text-slate-500">{item.currentChapterTitle ? `${item.currentChapterTitle} · ` : ''}{item.currentPage ? `page ${item.currentPage}${item.totalPages ? ` of ${item.totalPages}` : ''}` : ''}</p></Link></li>)}</ul>}
          </Card>
          <Card><CardHeader title="Recently added" action={<Link className="text-sm text-emerald-700 hover:underline dark:text-emerald-400" to="/library/resources?sortBy=added">Browse all</Link>} />
            {dashboard.recentlyAdded.length === 0 ? <EmptyState title="Your library is empty" description="Add a book, upload a PDF, save an external link or write a study note. No sample content is pre-loaded." />
              : <ul className="divide-y divide-slate-100 dark:divide-slate-800">{dashboard.recentlyAdded.map((resource) => <li key={resource.id}><Link to={`/library/resources/${resource.id}`} className="flex items-center gap-3 px-5 py-3 hover:bg-slate-50 dark:hover:bg-slate-800/60"><KindIcon resource={resource} /><span className="min-w-0 flex-1"><span className="block truncate text-sm font-medium">{resource.title}</span><span className="block truncate text-xs text-slate-500">{resource.authors ?? resource.sourceName ?? kindLabels[resource.kind]}</span></span><VisibilityBadge resource={resource} /></Link></li>)}</ul>}
          </Card>
          <Card className="xl:col-span-2"><CardHeader title="Recent bookmarks" action={<Link className="text-sm text-emerald-700 hover:underline dark:text-emerald-400" to="/library/reading">All bookmarks</Link>} />
            {dashboard.recentBookmarks.length === 0 ? <p className="px-5 py-6 text-sm text-slate-500">No bookmarks yet.</p>
              : <ul className="grid gap-2 p-4 sm:grid-cols-2 xl:grid-cols-3">{dashboard.recentBookmarks.map((bookmark) => <li key={bookmark.id}><Link to={`/library/resources/${bookmark.resourceId}${bookmark.page ? `?page=${bookmark.page}` : ''}`} className="flex items-start gap-2 rounded-lg border border-slate-200 p-3 text-sm hover:border-emerald-600 dark:border-slate-700"><BookOpen className="mt-0.5 h-4 w-4 shrink-0 text-emerald-600" /><span className="min-w-0"><span className="block truncate font-medium">{bookmark.label ?? bookmark.resourceTitle}</span><span className="block truncate text-xs text-slate-500">{bookmark.resourceTitle}{bookmark.chapterTitle ? ` · ${bookmark.chapterTitle}` : ''}{bookmark.page ? ` · p. ${bookmark.page}` : ''}</span></span></Link></li>)}</ul>}
          </Card>
        </div>
      </>}
    </>
  )
}

function Stat({ label, value, detail }: { label: string; value: number; detail: string }) {
  return <Card className="p-5"><p className="text-xs font-medium uppercase tracking-wide text-slate-500">{label}</p><p className="mt-2 text-2xl font-semibold">{value}</p><p className="mt-1 text-xs text-slate-500">{detail}</p></Card>
}
